using Amethyst.Common.Diagnostics;
using Amethyst.Common.Models;
using Amethyst.ModuleTweaker.Patching;
using AsmResolver.PE.File;
using AsmResolver.PE.Imports;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Newtonsoft.Json;
using System.Globalization;

namespace Amethyst.ModuleTweaker.Commands
{
    [Command(Description = "Patches or unpatches modules for runtime importing support.")]
    public class MainCommand : ICommand
    {
        [CommandOption("module", 'm', Description = "The specified module path to patch.")]
        public string ModulePath { get; set; } = null!;

        [CommandOption("symbols", 's', Description = "Path to directory containing *.symbols.json to use for patching.")]
        public string SymbolsPath { get; set; } = null!;

        public ValueTask ExecuteAsync(IConsole console)
        {
            FileInfo module = new(ModulePath);
            DirectoryInfo symbolsDir = new(SymbolsPath);
            if (module.Exists is false)
            {
                Logger.Warn("Couldn't patch module, specified module does not exist.");
                return default;
            }

            if (symbolsDir.Exists is false)
            {
                Logger.Warn("Couldn't patch module, specified symbols directory does not exist.");
                return default;
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

            //SymbolFactory.Register(new SymbolType(1, "function"), () => new PEFunctionSymbol());
            HeaderFactory.Register(new HeaderType(1, "pe32+"), (args) => new Patching.PE.V1.PEImporterHeader());

            // Collect all symbol files and accumulate mangled names
            IEnumerable<FileInfo> symbolFiles = symbolsDir.EnumerateFiles("*.json", SearchOption.AllDirectories);
            List<AbstractSymbol> symbols = [];
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
                            foreach (var function in symbolJson.Functions) {
                                if (string.IsNullOrEmpty(function.Name))
                                    continue;
                                symbols.Add(new Patching.PE.V1.PEFunctionSymbol {
                                    Name = function.Name,
                                    IsVirtual = false,
                                    IsSignature = function.Signature is not null,
                                    Address = ParseAddress(function.Address),
                                    Signature = function.Signature ?? string.Empty
                                });
                            }
                            foreach (var vfunc in symbolJson.VirtualFunctions) {
                                if (string.IsNullOrEmpty(vfunc.Name))
                                    continue;
                                symbols.Add(new Patching.PE.V1.PEFunctionSymbol {
                                    Name = vfunc.Name,
                                    IsVirtual = true,
                                    VirtualIndex = vfunc.Index,
                                    VirtualTable = vfunc.VirtualTable ?? "this"
                                });
                            }
                            foreach (var variable in symbolJson.Variables) {
                                if (string.IsNullOrEmpty(variable.Name))
                                    continue;
                                symbols.Add(new Patching.PE.V1.PEDataSymbol {
                                    Name = variable.Name,
                                    IsVirtualTable = false,
                                    Address = ParseAddress(variable.Address)
                                });
                            }
                            foreach (var vtable in symbolJson.VirtualTables) {
                                if (string.IsNullOrEmpty(vtable.Name))
                                    continue;
                                symbols.Add(new Patching.PE.V1.PEDataSymbol {
                                    Name = vtable.Name,
                                    IsVirtualTable = true,
                                    Address = ParseAddress(vtable.Address)
                                });
                            }
                            break;
                    }
                }
            }

            try
            {
                // Patch the module
                var file = PEFile.FromFile(ModulePath);
                var patcher = new Patching.PE.PEPatcher(file, symbols);
                if (patcher.Patch())
                {
                    File.Copy(ModulePath, ModulePath + ".bak", true);
                    file.Write(ModulePath);
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
