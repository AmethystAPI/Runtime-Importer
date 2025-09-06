using Amethyst.SymbolGenerator.Diagnostics;
using Amethyst.SymbolGenerator.Parsing;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Microsoft.VisualBasic.FileIO;
using ClangSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amethyst.SymbolGenerator.Extensions;
using Amethyst.SymbolGenerator.Tracking;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Amethyst.SymbolGenerator.Utility;
using Amethyst.SymbolGenerator.Parsing.Annotations;
using Newtonsoft.Json;
using Amethyst.SymbolGenerator.Models;

namespace Amethyst.SymbolGenerator.Commands
{
    [Command("generate", Description = "Generates symbol files based on the provided configuration.")]
    public partial class GenerateCommand : ICommand
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

            ArgumentNullException.ThrowIfNull(Input);
            ArgumentNullException.ThrowIfNull(Output);
            if (Input.Exists is false)
                throw new DirectoryNotFoundException($"Input directory '{Input.FullName}' does not exist.");
            
            // Ensure output directory exists
            Directory.CreateDirectory(Output.FullName);

            // Track changes in header files
            FileTracker headerTracker = null!;
            var (changes, checksums) = Utils.Benchmark<(FileChange[] changes, Dictionary<string, ulong> checksums)>("File Tracking", () =>
            {
                headerTracker = new(
                    inputDirectory: Input,
                    checksumFile: new FileInfo(Path.Combine(Output.FullName, "file_checksums.json")),
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

            List<RawAnnotation> annotations = [];
            ASTMethod[] methods = [];
            ASTVariable[] variables = [];
            bool hadErrors = Utils.Benchmark<bool>("AST Parsing", () =>
            {
                // Parse the generated .cpp file
                ASTVisitor visitor = new(
                    inputFile: generatedFile,
                    inputDirectory: Input.FullName,
                    arguments: Program.CompilerArguments
                );

                // Handle diagnostics
                if (visitor.PrintErrors())
                {
                    Logger.Error("Parsing failed due to errors.");
                    return true;
                }

                // Traverse the AST and collect classes, variables and methods
                _ = visitor.GetClasses();
                _ = visitor.GetVariables();
                _ = visitor.GetMethods();

                methods = [.. visitor.Methods];
                variables = [.. visitor.Variables];
                return false;
            });

            if (hadErrors)
            {
                return ValueTask.FromException(new Exception("Generation aborted due to errors."));
            }

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
                        Logger.Warning($"Skipping annotation for '{processed.Target.FullName}' due to unknown location.");
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
            return default;
        }
    }
}
