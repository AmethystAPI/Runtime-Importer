using Amethyst.Common.Diagnostics;
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
        [CommandOption("input", 'i', Description = "Path to the input directory containing header files.", IsRequired = true)]
        public string InputPath { get; set; } = null!;

        [CommandOption("output", 'o', Description = "Path to the output directory where symbol files will be generated.", IsRequired = true)]
        public string OutputPath { get; set; } = null!;

        [CommandOption("filters", 'f', Description = "List of filters to apply when generating symbols.")]
        public IReadOnlyList<string> Filters { get; set; } = null!;

        [CommandOption("platform", 'p', Description = "Target platform for symbol generation (e.g., win-client, win-server).", IsRequired = false)]
        public string Platform { get; set; } = "win-client";

        public ValueTask ExecuteAsync(IConsole console)
        {
            DirectoryInfo Input = new(InputPath);
            DirectoryInfo Output = new(OutputPath);
            if (Platform == "win-any")
                Logger.Fatal("Platform 'win-any' is not supported for symbol generation. Please specify either 'win-client' or 'win-server'.");

            if (!PlatformUtility.TryParse(Platform, out PlatformType PlatformType))
                Logger.Fatal($"Invalid platform '{Platform}'. Supported platforms are: win-client, win-server.");

            // Ensure input directory exists
            if (Input.Exists is false)
                Logger.Fatal($"Input directory '{Input.FullName}' does not exist.");
            
            // Ensure output directory exists
            Directory.CreateDirectory(Output.FullName);

            // Track changes in header files
            FileTracker headerTracker = null!;
            var (changes, checksums) = Utils.Benchmark<(FileChange[] changes, Dictionary<string, ulong> checksums)>("File Tracking", () =>
            {
                headerTracker = new(
                    inputDirectory: Input,
                    checksumFile: new FileInfo(Path.Combine(Output.FullName, $"{Platform}_header_checksums.json")),
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
            Dictionary<string, List<object>> annotationsData = [];

            if (addedOrModified.Length != 0)
            {
                // Prepare .cpp file for parsing
                string generatedFile = Path.Combine(Output.FullName, "Generated.cpp");
                List<string> willBeParsed = Utils.Benchmark<List<string>>("Prepare the Generated.cpp", () =>
                {
                    return [.. Utils.CreateIncludeFile(generatedFile, Input.FullName, addedOrModified)];
                });

                ASTMethod[] methods = [];
                ASTVariable[] variables = [];
                ASTClass[] classes = [];
                Utils.Benchmark("Parse the AST", () =>
                {
                    // Parse the generated .cpp file
                    ASTVisitor visitor = Utils.Benchmark<ASTVisitor>("Create the Translation Unit", () =>
                    {
                        return new(
                            inputFile: generatedFile,
                            inputDirectory: Input.FullName,
                            arguments: Program.CompilerArguments,
                            strictHeaders: willBeParsed
                        );
                    });

                    // Handle diagnostics
                    if (visitor.PrintErrors())
                    {
                        Logger.Fatal("Parsing failed due to errors.");
                        return;
                    }

                    // Traverse the AST and collect classes, variables and methods
                    Utils.Benchmark("Collect variables", () => visitor.GetVariables());
                    Utils.Benchmark("Collect classes", () => visitor.GetClasses());
                    Utils.Benchmark("Collect methods", () => visitor.GetMethods());

                    methods = [.. visitor.Methods];
                    variables = [.. visitor.Variables];
                    classes = [.. visitor.Classes];
                });

                Utils.Benchmark("Parse the comments", () =>
                {
                    // Extract annotations from variables
                    foreach (var variable in variables)
                    {
                        if (variable.RawComment is null || variable.Location is null || !willBeParsed.Contains(variable.Location.File))
                            continue;
                        RawAnnotation[] anns = CommentParser.ParseAnnotations(variable, variable.RawComment, variable.Location).ToArray();
                        annotations.AddRange(anns);
                    }

                    // Extract annotations from classes
                    foreach (var cls in classes)
                    {
                        if (cls.RawComment is null || cls.Location is null)
                            continue;
                        RawAnnotation[] anns = CommentParser.ParseAnnotations(cls, cls.RawComment, cls.Location).ToArray();
                        annotations.AddRange(anns);
                    }

                    // Extract annotations from non-virtual methods
                    foreach (var method in methods)
                    {
                        if (method.RawComment is null || method.Location is null)
                            continue;
                        RawAnnotation[] anns = CommentParser.ParseAnnotations(method, method.RawComment, method.Location).ToArray();
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

                        if (processed.Target.Location is not { } location || location.File == "Unknown File")
                        {
                            Logger.Warn($"Skipping annotation for '{processed.Target}' due to unknown location.");
                            continue;
                        }

                        if (!annotationsData.ContainsKey(location.File))
                            annotationsData[location.File] = [];
                        annotationsData[location.File].Add(processed.Data);

                        if (processed.Data is VirtualTableSymbolModel vtable && processed.Target is ASTClass cls)
                        {
                            // Create helper symbol $vtable_for_X$ for virtual tables
                            {
                                string[] reverseClassName = [.. cls.FullName.Split("::").Reverse(), ""];
                                string mangled = $"?$vtable_for_{vtable.ForWhat.Replace("::", "$")}@{string.Join("@", reverseClassName)}@2_KA";
                                annotationsData[location.File].Add(new VariableSymbolModel
                                {
                                    Name = mangled,
                                    Address = vtable.Address
                                });
                            }

                            // Add vtable symbol so MSVC keeps being happy
                            if (vtable.VtableMangledLabel is not null)
                            {
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

                // Generate output JSON files
                foreach (var (file, data) in annotationsData)
                {
                    string relativePath = Path.GetRelativePath(Input.FullName, file);
                    string outputFilePath = Path.Combine(Output.FullName, "symbols", Path.ChangeExtension(relativePath, "symbols.json"));
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);
                    string json = JsonConvert.SerializeObject(new SymbolJSONModel
                    {
                        FormatVersion = 1,
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
                    string relativePath = Path.GetRelativePath(Input.FullName, change.FilePath);
                    string outputFilePath = Path.Combine(Output.FullName, "symbols", Path.ChangeExtension(relativePath, "symbols.json"));
                    if (File.Exists(outputFilePath))
                    {
                        File.Delete(outputFilePath);
                        Logger.Debug($"Deleted: {outputFilePath}");
                    }
                }
            });

            // Save updated checksums only if all operations succeeded
            headerTracker.SaveChecksums(checksums);
            return default;
        }
    }
}
