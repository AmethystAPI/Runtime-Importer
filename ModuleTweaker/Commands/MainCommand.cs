using Amethyst.Common.Diagnostics;
using Amethyst.Common.Models;
using Amethyst.ModuleTweaker.Patching;
using Amethyst.ModuleTweaker.Patching.PE;
using Amethyst.ModuleTweaker.Patching.Symbols;
using AsmResolver.PE.File;
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
            DirectoryInfo symbols = new(SymbolsPath);
            if (module.Exists is false)
            {
                Logger.Warn("Couldn't patch module, specified module does not exist.");
                return default;
            }

            if (symbols.Exists is false)
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

            // Collect all symbol files and accumulate mangled names
            IEnumerable<FileInfo> symbolFiles = symbols.EnumerateFiles("*.json", SearchOption.AllDirectories);
            List<ImportedSymbol> importedSymbols = [];
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

                                importedSymbols.Add(new FunctionSymbol {
                                    Name = function.Name,
                                    IsVirtual = false,
                                    IsSignature = function.Signature is not null,
                                    Address = ParseAddress(function.Address),
                                    Signature = function.Signature ?? string.Empty
                                });
                            }
                            foreach (var variable in symbolJson.Variables)
                            {
                                if (string.IsNullOrEmpty(variable.Name))
                                    continue;
                                importedSymbols.Add(new VariableSymbol {
                                    Name = variable.Name,
                                    Address = ParseAddress(variable.Address)
                                });
                            }
                            foreach (var vtable in symbolJson.VirtualTables)
                            {
                                if (string.IsNullOrEmpty(vtable.Name))
                                    continue;
                                importedSymbols.Add(new VirtualPointerSymbol {
                                    Name = vtable.Name,
                                    Address = ParseAddress(vtable.Address)
                                });
                            }
                            foreach (var vfunc in symbolJson.VirtualFunctions)
                            {
                                if (string.IsNullOrEmpty(vfunc.Name))
                                    continue;
                                importedSymbols.Add(new FunctionSymbol {
                                    Name = vfunc.Name,
                                    IsVirtual = true,
                                    VirtualIndex = vfunc.Index,
                                    VirtualTable = vfunc.VirtualTable ?? "this"
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
                var patcher = new PEPatcher(file, importedSymbols);
                if (patcher.Patch())
                {
                    file.AlignSections();
                    File.Copy(ModulePath, ModulePath + ".backup", true);
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
