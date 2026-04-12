using Amethyst.Common.Diagnostics;
using Amethyst.Common.Extensions;
using Amethyst.Common.Models;
using Amethyst.Common.Tracking;
using Amethyst.Common.Utility;
using Amethyst.SymbolGenerator.Parsing;
using Amethyst.SymbolGenerator.Parsing.Annotations;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Newtonsoft.Json;
using System.IO;

namespace Amethyst.SymbolGenerator.Commands
{
    [Command(Description = "Generates symbol files based on the provided configuration.")]
    public partial class MainCommand : ICommand
    {
        [CommandOption("input", 'i', Description = "Paths to input directories containing header files. Can be specified multiple times.", IsRequired = true)]
        public IReadOnlyList<string> InputPaths { get; set; } = null!;

        [CommandOption("output", 'o', Description = "Path to the output directory where symbol files will be generated.", IsRequired = true)]
        public string OutputPath { get; set; } = null!;

        [CommandOption("filters", 'f', Description = "List of filters to apply when generating symbols.")]
        public IReadOnlyList<string> Filters { get; set; } = null!;

        [CommandOption("platform", 'p', Description = "Target platform for symbol generation (e.g., win-client, win-server).", IsRequired = false)]
        public string Platform { get; set; } = "win-client";

        /// <summary>
        /// Given an absolute file path, find which input directory it belongs to and return the relative path.
        /// </summary>
        private string GetRelativePath(DirectoryInfo[] inputs, string filePath)
        {
            string normalized = filePath.Replace('\\', '/');
            foreach (var input in inputs)
            {
                string inputNorm = input.FullName.Replace('\\', '/');
                if (!inputNorm.EndsWith('/')) inputNorm += '/';
                if (normalized.StartsWith(inputNorm, StringComparison.OrdinalIgnoreCase))
                    return Path.GetRelativePath(input.FullName, filePath);
            }
            return Path.GetRelativePath(inputs[0].FullName, filePath);
        }

        public ValueTask ExecuteAsync(IConsole console)
        {
            DirectoryInfo[] Inputs = [.. InputPaths.Select(p => new DirectoryInfo(p))];
            DirectoryInfo Output = new(OutputPath);
            if (Platform == "win-any")
                Logger.Fatal("Platform 'win-any' is not supported for symbol generation. Please specify either 'win-client' or 'win-server'.");

            if (!PlatformUtility.TryParse(Platform, out PlatformType PlatformType))
                Logger.Fatal($"Invalid platform '{Platform}'. Supported platforms are: win-client, win-server.");

            // Ensure all input directories exist
            foreach (var input in Inputs)
            {
                if (input.Exists is false)
                    Logger.Fatal($"Input directory '{input.FullName}' does not exist.");
            }

            // Ensure output directory exists
            Directory.CreateDirectory(Output.FullName);
            DirectoryInfo PlatformOutput = new(Path.Combine(Output.FullName, Platform));
            Directory.CreateDirectory(PlatformOutput.FullName);

            // Track changes in header files across all input directories
            FileTracker headerTracker = null!;
            var (changes, checksums) = Utils.Benchmark<(FileChange[] changes, Dictionary<string, ulong> checksums)>("File Tracking", () =>
            {
                headerTracker = new(
                    inputDirectories: Inputs,
                    checksumFile: new FileInfo(Path.Combine(PlatformOutput.FullName, $"header_checksums.json")),
                    searchPatterns: ["*.h", "*.hpp", "*.hh", "*.hxx"],
                    filters: [.. Filters]
                );
                return headerTracker.TrackChanges();
            });

            // Separate changes into added/modified and deleted
            FileChange[] addedOrModified = [.. changes
                .Where(c => c.ChangeType == ChangeType.Added || c.ChangeType == ChangeType.Modified)];
            FileChange[] deleted = [.. changes
                .Where(c => c.ChangeType == ChangeType.Deleted)];

            List<RawAnnotation> annotations = [];
            Dictionary<string, List<INamedSymbol>> annotationsData = [];

            ASTMethod[] methods = [];
            ASTVariable[] variables = [];
            ASTClass[] classes = [];
            AbstractAnnotationTarget[] targets = [];
            HashSet<string> failedFiles = [];
            if (addedOrModified.Length != 0)
            {
                // Prepare .cpp file for parsing
                string generatedFile = Path.Combine(PlatformOutput.FullName, "Generated.cpp");
                string[] inputFullNames = [.. Inputs.Select(i => i.FullName)];
                List<string> willBeParsed = Utils.Benchmark<List<string>>("Prepare the Generated.cpp", () =>
                {
                    return [.. Utils.CreateIncludeFile(generatedFile, inputFullNames, addedOrModified)];
                });

                Utils.Benchmark("Parse the AST", () =>
                {
                    // Parse the generated .cpp file
                    ASTVisitor visitor = Utils.Benchmark<ASTVisitor>("Create the Translation Unit", () =>
                    {
                        return new(
                            inputFile: generatedFile,
                            inputDirectories: inputFullNames,
                            arguments: Program.CompilerArguments,
                            strictHeaders: willBeParsed
                        );
                    });

                    // Log diagnostics and exclude files with errors from strict headers
                    var errorFiles = visitor.GetFilesWithErrors();
                    foreach (var errorFile in errorFiles)
                        visitor.StrictHeaders.Remove(errorFile);
                    failedFiles = errorFiles;

                    // If fatal errors occurred (e.g. "too many errors"), the AST is too
                    // corrupt to safely traverse — skip it entirely to avoid crashes.
                    // Mark ALL files as failed so none get cached as "processed".
                    if (visitor.HasFatalErrors)
                    {
                        Logger.Warn("Skipping AST traversal due to fatal parse errors. Fix the source errors and rebuild.");
                        foreach (var file in willBeParsed)
                            failedFiles.Add(Path.GetFullPath(file).NormalizeSlashes());
                        return;
                    }

                    // Traverse the AST and collect classes, variables and methods
                    Utils.Benchmark("Collect variables", () => visitor.GetVariables());
                    Utils.Benchmark("Collect classes", () => visitor.GetClasses());
                    Utils.Benchmark("Collect methods", () => visitor.GetMethods());

                    methods = [.. visitor.Methods];
                    variables = [.. visitor.Variables];
                    classes = [.. visitor.Classes];
                    targets = [.. methods, .. variables, .. classes];
                });

                Utils.Benchmark("Parse the comments", () =>
                {
                    foreach (var target in targets)
                    {
                        if (target.RawComment is null || target.Location is null)
                            continue;
                        RawAnnotation[] anns = CommentParser.ParseAnnotations(target, target.RawComment, target.Location).ToArray();
                        annotations.AddRange(anns);
                    }
                });

                Utils.Benchmark("Process annotations", () =>
                {
                    // Process extracted annotations
                    var processor = new AnnotationProcessor(PlatformType);
                    Utils.Benchmark("Process and resolve annotations", () => processor.ProcessAndResolve(annotations));
                    foreach (var processed in processor.ProcessedAnnotations)
                    {
                        if (processed is null)
                            continue;

                        if (processed.Data is not INamedSymbol symbol) {
                            Logger.Debug($"Silently skipping '{processed.Target.IdentificationName}' due to invalid data type.");
                            continue;
                        }

                        if (processed.Target.Location is not { } location || location.File == "<unknown>") {
                            Logger.Warn($"Skipping annotation for '{processed.Target}' due to unknown location.");
                            continue;
                        }

                        if (!annotationsData.ContainsKey(location.File))
                            annotationsData[location.File] = [];
                        // Kinda hacky, will change later
                        annotationsData[location.File].Add(symbol);

                        if (processed.Data is VirtualTableSymbolModel vtable && processed.Target is ASTClass cls)
                        {
                            // Create helper symbol $vtable_for_X$ for virtual tables
                            foreach (var variable in cls.Variables.Where(v => v.MangledName.StartsWith($"?$vtable_for_{vtable.ForWhat.Replace("::", "$")}"))) {
                                bool exists = processor.ProcessedAnnotations.Select(a => a.Data).OfType<VariableSymbolModel>().FirstOrDefault(v => v.Name == variable.MangledName) is not null;
                                if (exists)
                                    continue;
                                annotationsData[location.File].Add(new VariableSymbolModel {
                                    Name = variable.MangledName,
                                    Address = vtable.Address,
                                    Signature = vtable.Signature,
                                    IsVirtualTableAddress = true
                                });
                            }

                            // Add vtable symbol so MSVC keeps being happy
                            if (vtable.VtableMangledLabel is not null) {
                                annotationsData[location.File].Add(new VariableSymbolModel
                                {
                                    Name = vtable.VtableMangledLabel,
                                    Address = vtable.Address
                                });
                            }
                        }
                    }
                });
            }

            Utils.Benchmark("Generate symbols", () =>
            {
                JsonSerializerSettings jsonSettings = new()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                };

                string templatePredefined = Path.Combine(PlatformOutput.FullName, "symbols", "template.pregenerated.symbols.json");
                if (File.Exists(templatePredefined)) {
                    string json = File.ReadAllText(templatePredefined);
                    var predefinedSymbols = JsonConvert.DeserializeObject<SymbolJSONModel>(json, jsonSettings);
                    if (predefinedSymbols is not null)
                    {
                        INamedSymbol[] namedSymbols = [
                            ..predefinedSymbols.Functions,
                            ..predefinedSymbols.Variables,
                            ..predefinedSymbols.VirtualTables,
                            ..predefinedSymbols.VirtualFunctions
                        ];

                        foreach (var symbol in namedSymbols)
                        {
                            if (targets.FirstOrDefault(t => t.IsNamedAs(symbol.Name)) is { } target) {

                                if (!target.IsImported || annotationsData.SelectMany(kv => kv.Value).Any(v => v is not null && v.Name == symbol.Name))
                                    continue;
                                string file = target.Location?.File ?? "<unknown>";
                                if (!annotationsData.ContainsKey(file))
                                    annotationsData[file] = [];
                                symbol.Name = target.IdentificationName;
                                annotationsData[file].Add(symbol);
                            }
                        }
                    }
                }

                // Generate output JSON files
                foreach (var (file, data) in annotationsData)
                {
                    string relativePath = GetRelativePath(Inputs, file);
                    string outputFilePath = Path.Combine(PlatformOutput.FullName, "symbols", Path.ChangeExtension(relativePath, "symbols.json"));
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);
                    string json = JsonConvert.SerializeObject(new SymbolJSONModel
                    {
                        Functions = [.. data.OfType<FunctionSymbolModel>()],
                        Variables = [.. data.OfType<VariableSymbolModel>()],
                        VirtualTables = [.. data.OfType<VirtualTableSymbolModel>().DistinctBy(k => k.Name)],
                        VirtualFunctions = [.. data.OfType<VirtualFunctionSymbolModel>()],
                    }, Formatting.Indented, jsonSettings);
                    File.WriteAllText(outputFilePath, json);
                    Logger.Debug($"Generated: {outputFilePath}");
                }

                // Handle deleted files
                foreach (var change in deleted)
                {
                    string relativePath = GetRelativePath(Inputs, change.FilePath);
                    string outputFilePath = Path.Combine(PlatformOutput.FullName, "symbols", Path.ChangeExtension(relativePath, "symbols.json"));
                    if (File.Exists(outputFilePath))
                    {
                        File.Delete(outputFilePath);
                        Logger.Debug($"Deleted: {outputFilePath}");
                    }
                }
            });

            // Remove failed files from checksums so they get re-processed next run
            foreach (var file in failedFiles)
            {
                string normalized = file.Replace('\\', '/');
                var key = checksums.Keys.FirstOrDefault(k => k.Replace('\\', '/').Equals(normalized, StringComparison.OrdinalIgnoreCase));
                if (key is not null)
                    checksums.Remove(key);
            }

            headerTracker.SaveChecksums(checksums);
            return default;
        }
    }
}
