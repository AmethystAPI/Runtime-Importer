using Amethyst.Common.Diagnostics;
using Amethyst.Common.Extensions;
using Amethyst.Common.Models;
using Amethyst.Common.Tracking;
using Amethyst.Common.Utility;
using Amethyst.SymbolGenerator.Linking;
using Amethyst.SymbolGenerator.Parsing;
using Amethyst.SymbolGenerator.Parsing.Annotations;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Newtonsoft.Json;
using System.Diagnostics;
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
                return headerTracker.TrackChanges(retainContent: true);
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

                // (no per-header JSON emit; everything lives in the cache below)
            });

            // Load the unified symbol cache, merge this run's results in.
            FileInfo cacheFile = new(Path.Combine(PlatformOutput.FullName, "symbol_cache.json"));
            SymbolCache cache = Utils.Benchmark<SymbolCache>("Load symbol cache", () => SymbolCache.Load(cacheFile));
            bool cacheDirty = false;

            foreach (var (file, data) in annotationsData)
            {
                cache.Entries[file] = new SymbolJSONModel
                {
                    Functions = [.. data.OfType<FunctionSymbolModel>()],
                    Variables = [.. data.OfType<VariableSymbolModel>()],
                    VirtualTables = [.. data.OfType<VirtualTableSymbolModel>().DistinctBy(k => k.Name)],
                    VirtualFunctions = [.. data.OfType<VirtualFunctionSymbolModel>()],
                };
                cacheDirty = true;
            }

            foreach (var change in deleted)
            {
                if (cache.Entries.Remove(change.FilePath))
                    cacheDirty = true;
            }

            if (cacheDirty)
                Utils.Benchmark("Save symbol cache", () => cache.Save(cacheFile));

            // Build the lib (inline; replaces the separate LibraryGenerator process).
            Utils.Benchmark("Generate library", () => GenerateLibrary(PlatformOutput, cache, cacheDirty));

            // Keep failed files in checksums; they'll be retried when their content actually changes.
            headerTracker.SaveChecksums(checksums);
            return default;
        }

        private void GenerateLibrary(DirectoryInfo platformOutput, SymbolCache cache, bool cacheDirty)
        {
            // Union of all mangled names across cache + pregenerated.symbols.json overrides.
            HashSet<string> allMangledNames = [];
            foreach (var entry in cache.Entries.Values)
            {
                foreach (var f in entry.Functions) if (!string.IsNullOrEmpty(f.Name)) allMangledNames.Add(f.Name);
                foreach (var v in entry.Variables) if (!string.IsNullOrEmpty(v.Name)) allMangledNames.Add(v.Name);
                foreach (var vf in entry.VirtualFunctions) if (!string.IsNullOrEmpty(vf.Name)) allMangledNames.Add(vf.Name);
            }

            // Pregen file (rare, hand-curated symbols the auto-extractor can't reach).
            string pregenPath = Path.Combine(platformOutput.FullName, "symbols", "pregenerated.symbols.json");
            if (File.Exists(pregenPath))
            {
                var pre = JsonConvert.DeserializeObject<SymbolJSONModel>(File.ReadAllText(pregenPath));
                if (pre is not null)
                {
                    foreach (var f in pre.Functions) if (!string.IsNullOrEmpty(f.Name)) allMangledNames.Add(f.Name);
                    foreach (var v in pre.Variables) if (!string.IsNullOrEmpty(v.Name)) allMangledNames.Add(v.Name);
                    foreach (var vf in pre.VirtualFunctions) if (!string.IsNullOrEmpty(vf.Name)) allMangledNames.Add(vf.Name);
                }
            }

            // Skip the (slow) lib.exe invocations when nothing actually changed AND a lib already exists.
            string[] existingLibs = Directory.Exists(platformOutput.FullName)
                ? [.. Directory.EnumerateFiles(platformOutput.FullName, "Minecraft.Windows.*.lib")]
                : [];
            if (!cacheDirty && existingLibs.Length > 0)
            {
                Logger.Debug("Symbol cache unchanged and a lib already exists, skipping lib.exe.");
                return;
            }

            // Clear old chunked artifacts.
            foreach (var f in Directory.EnumerateFiles(platformOutput.FullName, "Minecraft.Windows.*.def")) File.Delete(f);
            foreach (var f in existingLibs) File.Delete(f);

            int index = 0;
            List<Process> libProcs = [];
            foreach (var chunk in allMangledNames.ChunkBy(65500))
            {
                string defFilePath = Path.Combine(platformOutput.FullName, $"Minecraft.Windows.{index}.def");
                string libFilePath = Path.Combine(platformOutput.FullName, $"Minecraft.Windows.{index}.lib");
                Utils.CreateDefinitionFile(defFilePath, chunk);
                libProcs.Add(Lib.GenerateLib(defFilePath, libFilePath));
                index++;
            }

            for (int i = 0; i < libProcs.Count; i++)
            {
                libProcs[i].WaitForExit();
                if (libProcs[i].ExitCode != 0)
                    Logger.Fatal($"lib.exe failed on chunk {i}, output: {libProcs[i].StandardError.ReadToEnd()}");
                File.Delete(Path.Combine(platformOutput.FullName, $"Minecraft.Windows.{i}.exp"));
            }
        }
    }
}
