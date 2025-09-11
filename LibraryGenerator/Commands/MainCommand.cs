using Amethyst.Common.Diagnostics;
using Amethyst.Common.Tracking;
using Amethyst.Common.Models;
using Amethyst.LibraryGenerator.Tools.MSVC;
using Amethyst.Common.Utility;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Newtonsoft.Json;

namespace Amethyst.LibraryGenerator.Commands
{
    [Command(Description = "Generates a library file based on the provided symbol files.")]
    public class MainCommand : ICommand
    {
        [CommandOption("input", 'i', Description = "Path to the input directory containing *.symbol.json files.", IsRequired = true)]
        public string InputPath { get; set; } = null!;

        [CommandOption("output", 'o', Description = "Path to the output directory where the lib will be generated.", IsRequired = true)]
        public string OutputPath { get; set; } = null!;

        public ValueTask ExecuteAsync(IConsole console)
        {
            DirectoryInfo Input = new(InputPath);
            DirectoryInfo Output = new(OutputPath);

            if (Input.Exists is false)
            {
                Logger.Warn("Couldn't generate library, input directory does not exist.");
                return default;
            }

            // Ensure output directory exists
            Directory.CreateDirectory(Output.FullName);

            // Track changes in symbol files
            FileTracker symbolTracker = null!;
            var (hadChanges, checksums) = Utils.Benchmark<(bool hadChanges, Dictionary<string, ulong> checksums)>("File Tracking", () =>
            {
                symbolTracker = new(
                    inputDirectory: Input,
                    checksumFile: new FileInfo(Path.Combine(Output.FullName, "symbols_checksums.json")),
                    searchPatterns: ["*.json"],
                    filters: []
                );
                var changes = symbolTracker.TrackChanges();
                return (changes.Changes.Length != 0, changes.NewChecksums);
            });

            if (!hadChanges)
            {
                Logger.Info("No '*.symbols.json' changes detected, no lib to generate.");
                return default;
            }

            // Collect all symbol files and accumulate mangled names
            IEnumerable<FileInfo> symbolFiles = Input.EnumerateFiles("*.json", SearchOption.AllDirectories);
            HashSet<string> allMangledNames = [];
            foreach (var symbolFile in symbolFiles)
            {
                using var stream = symbolFile.OpenRead();
                using var sr = new StreamReader(stream);
                SymbolJSONModel? symbolJson = JsonConvert.DeserializeObject<SymbolJSONModel>(sr.ReadToEnd());
                if (symbolJson is not null)
                {
                    switch (symbolJson.FormatVersion)
                    {
                        case 1:
                            foreach (var function in symbolJson.Functions)
                            {
                                if (string.IsNullOrEmpty(function.Name))
                                    continue;
                                Logger.Info("Adding symbol: " + function.Name);
                                allMangledNames.Add(function.Name);
                            }

                            foreach (var variable in symbolJson.Variables)
                            {
                                if (string.IsNullOrEmpty(variable.Name))
                                    continue;
                                Logger.Info("Adding symbol: " + variable.Name);
                                allMangledNames.Add(variable.Name);
                            }

                            foreach (var vfunc in symbolJson.VirtualFunctions)
                            {
                                if (string.IsNullOrEmpty(vfunc.Name))
                                    continue;
                                Logger.Info("Adding symbol: " + vfunc.Name);
                                allMangledNames.Add(vfunc.Name);
                            }
                            break;
                    }
                }
            }

            // .def and .lib file paths
            string defFilePath = Path.Combine(OutputPath, "Minecraft.Windows.def");
            string libFilePath = Path.Combine(OutputPath, "Minecraft.Windows.lib");

            // Create .def file
            Utils.CreateDefinitionFile(defFilePath, allMangledNames);

            // Generate .lib file
            var libProc = Lib.GenerateLib(defFilePath, libFilePath);
            libProc.WaitForExit();
            if (libProc.ExitCode != 0)
            {
                return ValueTask.FromException(new Exception("Library generation aborted due to errors."));
            }

            // Save updated checksums only if all operations succeeded
            symbolTracker.SaveChecksums(checksums);
            Logger.Info("Library 'Minecraft.Windows.lib' generated succesfully.");
            return default;
        }
    }
}
