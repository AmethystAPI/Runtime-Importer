using Amethyst.Common.Diagnostics;
using Amethyst.SymbolGenerator.Parsing;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Amethyst.Common.Tracking;
using Amethyst.Common.Utility;
using Amethyst.SymbolGenerator.Parsing.Annotations;
using Newtonsoft.Json;
using Amethyst.Common.Models;

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

        public ValueTask ExecuteAsync(IConsole console)
        {
            DirectoryInfo Input = new(InputPath);
            DirectoryInfo Output = new(OutputPath);

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
                    checksumFile: new FileInfo(Path.Combine(Output.FullName, "header_checksums.json")),
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

            // Prepare .cpp file for parsing
            string generatedFile = Path.Combine(Output.FullName, "Generated.cpp");
            List<string> willBeParsed = Utils.CreateIncludeFile(generatedFile, Input.FullName, addedOrModified).ToList();

            ASTMethod[] methods = [];
            ASTVariable[] variables = [];
            Utils.Benchmark("AST Parsing", () =>
            {
                // Parse the generated .cpp file
                ASTVisitor visitor = new(
                    inputFile: generatedFile,
                    inputDirectory: Input.FullName,
                    arguments: Program.CompilerArguments,
                    strictHeaders: willBeParsed
                );

                // Handle diagnostics
                if (visitor.PrintErrors())
                {
                    Logger.Fatal("Parsing failed due to errors.");
                    return;
                }

                // Traverse the AST and collect classes, variables and methods
                _ = visitor.GetClasses();
                _ = visitor.GetVariables();
                _ = visitor.GetMethods();

                methods = [.. visitor.Methods];
                variables = [.. visitor.Variables];
            });

            List<RawAnnotation> annotations = [];
            Utils.Benchmark("Comment Parsing", () =>
            {
                // Extract annotations from methods
                foreach (var method in methods)
                {
                    if (method.RawComment is null || method.Location is null || !willBeParsed.Contains(method.Location.File))
                        continue;
                    RawAnnotation[] anns = CommentParser.ParseAnnotations(method.RawComment, method.Location ?? new("Unknown File", 0, 0, 0)).ToArray();
                    for (int i = 0; i < anns.Length; i++)
                    {
                        anns[i].Target = method;
                    }
                    annotations.AddRange(anns);
                }

                // Extract annotations from variables
                foreach (var variable in variables)
                {
                    if (variable.RawComment is null || variable.Location is null || !willBeParsed.Contains(variable.Location.File))
                        continue;
                    RawAnnotation[] anns = CommentParser.ParseAnnotations(variable.RawComment, variable.Location ?? new("Unknown File", 0, 0, 0)).ToArray();
                    for (int i = 0; i < anns.Length; i++)
                    {
                        anns[i].Target = variable;
                    }
                    annotations.AddRange(anns);
                }
            });

            Dictionary<string, List<object>> annotationsData = [];
            Utils.Benchmark("Process Annotations", () =>
            {
                // Process extracted annotations
                var processedAnnotations = AnnotationProcessor.ProcessAnnotations(annotations);
                foreach (var processed in processedAnnotations)
                {
                    if (processed is null)
                        continue;

                    if (processed.Target.Location is not { } location || location.File == "Unknown File")
                    {
                        Logger.Warn($"Skipping annotation for '{processed.Target.FullName}' due to unknown location.");
                        continue;
                    }

                    if (!annotationsData.ContainsKey(location.File))
                        annotationsData[location.File] = [];
                    annotationsData[location.File].Add(processed.Data);
                }
            });

            Utils.Benchmark("Generate Output", () =>
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
                        Functions = [.. data.OfType<MethodSymbolJSONModel>()],
                        Variables = [.. data.OfType<VariableSymbolJSONModel>()]
                    }, Formatting.Indented, jsonSettings);
                    File.WriteAllText(outputFilePath, json);
                    Logger.Info($"Generated: {outputFilePath}");
                }

                // Handle deleted files
                foreach (var change in deleted)
                {
                    string relativePath = Path.GetRelativePath(Input.FullName, change.FilePath);
                    string outputFilePath = Path.Combine(Output.FullName, "symbols", Path.ChangeExtension(relativePath, "symbols.json"));
                    if (File.Exists(outputFilePath))
                    {
                        File.Delete(outputFilePath);
                        Logger.Info($"Deleted: {outputFilePath}");
                    }
                }
            });

            // Save updated checksums only if all operations succeeded
            headerTracker.SaveChecksums(checksums);
            Logger.Info("Symbols generated succesfully.");
            return default;
        }
    }
}
