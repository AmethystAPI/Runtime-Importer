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

        [CommandOption("platform", 'p', Description = "Target platform for symbol generation (e.g., win-client, win-server).", IsRequired = false)]
        public string Platform { get; set; } = "win-client";

        [CommandOption("pregen-sym", Description = "Overrides the default pregenerated.symbols.json file folder.")]
        public string? PregeneratedSymbolsPath { get; set; }

        public ValueTask ExecuteAsync(IConsole console)
        {
            if (!PlatformUtility.TryParse(Platform, out PlatformType PlatformType))
                Logger.Fatal($"Invalid platform '{Platform}'. Supported platforms are: win-client, win-server.");

            DirectoryInfo Input = new(InputPath);
            DirectoryInfo Output = new(OutputPath);

            if (Input.Exists is false)
            {
                Logger.Warn("Couldn't generate library, input directory does not exist.");
                return default;
            }

            DirectoryInfo PlatformInput = new(Path.Combine(Input.FullName, Platform));
            if (PlatformInput.Exists is false)
            {
                Logger.Warn($"Couldn't generate library, platform-specific input directory '{PlatformInput.FullName}' does not exist.");
                return default;
            }

            DirectoryInfo PlatformSymbolInput = new(Path.Combine(PlatformInput.FullName, "symbols"));
            if (PlatformSymbolInput.Exists is false) {
                Logger.Warn($"Couldn't generate library, platform-specific symbols directory '{PlatformSymbolInput.FullName}' does not exist.");
                return default;
            }

            // Ensure output directory exists
            Directory.CreateDirectory(Output.FullName);
            DirectoryInfo PlatformOutput = new(Path.Combine(Output.FullName, Platform));
            Directory.CreateDirectory(PlatformOutput.FullName);

            // Track changes in symbol files
            FileTracker symbolTracker = null!;
            var (hadChanges, checksums) = Utils.Benchmark<(bool hadChanges, Dictionary<string, ulong> checksums)>("File Tracking", () =>
            {
                symbolTracker = new(
                    inputDirectory: PlatformSymbolInput,
                    checksumFile: new FileInfo(Path.Combine(PlatformOutput.FullName, $"symbols_checksums.json")),
                    searchPatterns: ["*.json"],
                    filters: []
                );
                var changes = symbolTracker.TrackChanges();
                return (changes.Changes.Length != 0, changes.NewChecksums);
            });

            if (!hadChanges)
            {
                Logger.Debug("No '*.symbols.json' changes detected, no lib to generate.");
                return default;
            }

            // Collect all symbol files and accumulate mangled names
            IEnumerable<FileInfo> symbolFiles = PlatformSymbolInput
                .EnumerateFiles("*.symbols.json", SearchOption.AllDirectories)
                .Where(f => Path.GetFileName(f.FullName) != "pregenerated.symbols.json");

            string pregeneratedPath = PregeneratedSymbolsPath is null ? 
                Path.Combine(PlatformSymbolInput.FullName, "pregenerated.symbols.json") :
                PregeneratedSymbolsPath;
            if (File.Exists(pregeneratedPath)) {
                symbolFiles = symbolFiles.Prepend(new FileInfo(pregeneratedPath));
            }

            HashSet<string> allMangledNames = [];
            foreach (var symbolFile in symbolFiles)
            {
                using var stream = symbolFile.OpenRead();
                using var sr = new StreamReader(stream);
                SymbolJSONModel? symbolJson = JsonConvert.DeserializeObject<SymbolJSONModel>(sr.ReadToEnd());
                if (symbolJson is not null)
                {
                    foreach (var function in symbolJson.Functions) {
                        if (string.IsNullOrEmpty(function.Name))
                            continue;
                        allMangledNames.Add(function.Name);
                    }

                    foreach (var variable in symbolJson.Variables) {
                        if (string.IsNullOrEmpty(variable.Name))
                            continue;
                        allMangledNames.Add(variable.Name);
                    }

                    foreach (var vfunc in symbolJson.VirtualFunctions) {
                        if (string.IsNullOrEmpty(vfunc.Name))
                            continue;
                        allMangledNames.Add(vfunc.Name);
                    }
                }
            }

            int index = 0;
            foreach (var chunk in allMangledNames.ChunkBy(65534))
            {
                // .def and .lib file paths
                string defFilePath = Path.Combine(PlatformOutput.FullName, $"Minecraft.Windows.{index}.def");
                string libFilePath = Path.Combine(PlatformOutput.FullName, $"Minecraft.Windows.{index}.lib");

                // Create .def file
                Utils.CreateDefinitionFile(defFilePath, chunk);

                // Generate .lib file
                var libProc = Lib.GenerateLib(defFilePath, libFilePath);
                libProc.WaitForExit();
                if (libProc.ExitCode != 0) {
                    return ValueTask.FromException(new Exception("Library generation aborted due to errors."));
                }

                Logger.Debug($"Library 'Minecraft.Windows.{index}.lib' generated succesfully.");
                File.Delete(Path.ChangeExtension(defFilePath, ".exp")); // Clean up .exp file
                index++;
            }

            // Save updated checksums only if all operations succeeded
            symbolTracker.SaveChecksums(checksums);
            return default;
        }
    }
}
