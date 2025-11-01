using Amethyst.Common.Diagnostics;
using Amethyst.Common.Models;
using Amethyst.ModuleTweaker.Patching;
using AsmResolver.PE.File;
using AsmResolver.PE.Imports;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using K4os.Hash.xxHash;
using Newtonsoft.Json;
using System;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Amethyst.ModuleTweaker.Commands
{
    [Command(Description = "Patches or unpatches modules for runtime importing support.")]
    public class MainCommand : ICommand
    {
        [CommandOption("module", 'm', Description = "The specified module path to patch.", IsRequired = true)]
        public string ModulePath { get; set; } = null!;

        [CommandOption("input", 'i', Description = "Path to input directory to use for patching.", IsRequired = true)]
        public string InputPath { get; set; } = null!;

        [CommandOption("output", 'o', Description = "Path to save temporary files, don't confuse with -m.")]
        public string OutputPath { get; set; } = null!;

        [CommandOption("platform", 'p', Description = "Target platform for symbol generation (e.g., win-client, win-server).", IsRequired = false)]
        public string Platform { get; set; } = "win-client";

        [CommandOption("pregen-sym", Description = "Overrides the default pregenerated.symbols.json file folder.")]
        public string? PregeneratedSymbolsPath { get; set; }

        public ValueTask ExecuteAsync(IConsole console)
        {
            FileInfo module = new(ModulePath);
            DirectoryInfo Input = new(InputPath);
            if (module.Exists is false) {
                Logger.Fatal("Couldn't patch module, specified module does not exist.");
                return default;
            }

            if (Input.Exists is false) {
                Logger.Fatal("Couldn't patch module, specified input directory does not exist.");
                return default;
            }

            DirectoryInfo PlatformInput = new(Path.Combine(Input.FullName, Platform));
            if (PlatformInput.Exists is false) {
                Logger.Warn($"Couldn't patch module, platform-specific input directory '{PlatformInput.FullName}' does not exist.");
                return default;
            }

            DirectoryInfo PlatformSymbolInput = new(Path.Combine(PlatformInput.FullName, "symbols"));
            if (PlatformSymbolInput.Exists is false) {
                Logger.Warn($"Couldn't patch module, platform-specific symbols directory '{PlatformSymbolInput.FullName}' does not exist.");
                return default;
            }

            if (string.IsNullOrEmpty(OutputPath)) {
                OutputPath = Path.GetFullPath(Path.Combine(Input.FullName, "../"));
            }
            DirectoryInfo Output = new(OutputPath);
            Directory.CreateDirectory(Output.FullName);
            DirectoryInfo PlatformOutput = new(Path.Combine(Output.FullName, Platform));
            Directory.CreateDirectory(PlatformOutput.FullName);

            var bytes = File.ReadAllBytes(ModulePath);
            ulong hash = XXH64.DigestOf(bytes);
            if (File.Exists(Path.Combine(PlatformOutput.FullName, "module_hash.txt"))) {
                var existingHash = File.ReadAllText(Path.Combine(PlatformOutput.FullName, "module_hash.txt"));
                if (ulong.TryParse(existingHash, NumberStyles.HexNumber, null, out var existingHashValue)) {
                    if (existingHashValue == hash) {
                        Logger.Info("Module hash matches previous hash, skipping patch.");
                        return default;
                    }
                }
            }

            ulong ParseAddress(string? address)
            {
                if (string.IsNullOrEmpty(address))
                    return 0x0;
                if (address.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    address = address[2..];
                if (!ulong.TryParse(address, NumberStyles.HexNumber, null, out var addr))
                    return 0x0;
                return addr;
            }

            SymbolFactory.Register(new SymbolType(1, "pe32+", "data"), () => new Patching.PE.V1.PEDataSymbol());
            SymbolFactory.Register(new SymbolType(2, "pe32+", "data"), () => new Patching.PE.V2.PEDataSymbol());
            SymbolFactory.Register(new SymbolType(1, "pe32+", "function"), () => new Patching.PE.V1.PEFunctionSymbol());
            HeaderFactory.Register(new HeaderType(1, "pe32+"), (args) => new Patching.PE.V1.PEImporterHeader());

            // Collect all symbol files and accumulate mangled names
            IEnumerable<FileInfo> symbolFiles = PlatformSymbolInput
                .EnumerateFiles("*.symbols.json", SearchOption.AllDirectories)
                .Where(f => Path.GetFileName(f.FullName) != "pregenerated.symbols.json" && Path.GetFileName(f.FullName) != "template.pregenerated.symbols.json");

            string pregeneratedPath = PregeneratedSymbolsPath is null ? 
                Path.Combine(PlatformSymbolInput.FullName, "pregenerated.symbols.json") :
                PregeneratedSymbolsPath;
            if (File.Exists(pregeneratedPath)) {
                symbolFiles = symbolFiles.Prepend(new FileInfo(pregeneratedPath));
            }

            Dictionary<string, AbstractSymbol> symbols = [];
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
                        symbols[function.Name] = new Patching.PE.V1.PEFunctionSymbol {
                            Name = function.Name,
                            IsVirtual = false,
                            IsSignature = function.Signature is not null,
                            Address = ParseAddress(function.Address),
                            Signature = function.Signature ?? string.Empty
                        };
                    }
                    foreach (var vfunc in symbolJson.VirtualFunctions) {
                        if (string.IsNullOrEmpty(vfunc.Name))
                            continue;
                        symbols[vfunc.Name] = new Patching.PE.V1.PEFunctionSymbol {
                            Name = vfunc.Name,
                            IsVirtual = true,
                            VirtualIndex = vfunc.Index,
                            VirtualTable = vfunc.VirtualTable ?? "this",
                            IsDestructor = vfunc.IsVirtualDestructor,
                            HasStorage = vfunc.IsVirtualDestructor
                        };
                    }
                    foreach (var variable in symbolJson.Variables) {
                        if (string.IsNullOrEmpty(variable.Name))
                            continue;
                        symbols[variable.Name] = new Patching.PE.V2.PEDataSymbol {
                            Name = variable.Name,
                            IsVirtualTable = false,
                            Address = ParseAddress(variable.Address),
                            IsVirtualTableAddress = variable.IsVirtualTableAddress,
                            HasStorage = variable.IsVirtualTableAddress,
                            IsSignature = variable.Signature is not null,
                            Signature = variable.Signature ?? string.Empty
                        };
                    }
                    foreach (var vtable in symbolJson.VirtualTables) {
                        if (string.IsNullOrEmpty(vtable.Name))
                            continue;
                        symbols[vtable.Name] = new Patching.PE.V2.PEDataSymbol {
                            Name = vtable.Name,
                            IsVirtualTable = true,
                            Address = ParseAddress(vtable.Address),
                            IsSignature = vtable.Signature is not null,
                            Signature = vtable.Signature ?? string.Empty
                        };
                    }
                }
            }

            try
            {
                // Patch the module
                var peFile = PEFile.FromBytes(bytes);
                if (peFile is null) {
                    Logger.Fatal("Failed to read module as a PE file.");
                    return default;
                }
                Logger.Info($"Loaded module '{ModulePath}' as PE file.");
                var patcher = new Patching.PE.PEPatcher(peFile, [..symbols.Values]);

                if (patcher.Patch())
                {
                    File.Copy(ModulePath, ModulePath + ".bak", true);
                    using var ms = new MemoryStream();
                    peFile.Write(ms);
                    var newBytes = ms.ToArray();
                    ulong newHash = XXH64.DigestOf(newBytes);
                    File.WriteAllBytes(ModulePath, newBytes);
                    File.WriteAllText(Path.Combine(PlatformOutput.FullName, "module_hash.txt"), newHash.ToString("X16"));
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Failed to patch module '{ModulePath}': {ex}");
                return default;
            }
            return default;
        }
    }
}
