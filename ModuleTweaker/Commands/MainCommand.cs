using Amethyst.Common.Diagnostics;
using Amethyst.Common.Models;
using Amethyst.ModuleTweaker.Patching;
using AsmResolver.PE.File;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Newtonsoft.Json;

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

            // Collect all symbol files and accumulate mangled names
            IEnumerable<FileInfo> symbolFiles = symbols.EnumerateFiles("*.json", SearchOption.AllDirectories);
            HashSet<MethodSymbolJSONModel> methods = [];
            HashSet<VariableSymbolJSONModel> variables = [];
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
                                methods.Add(function);
                            }
                            foreach (var variable in symbolJson.Variables)
                            {
                                if (string.IsNullOrEmpty(variable.Name))
                                    continue;
                                variables.Add(variable);
                            }
                            break;
                    }
                }
            }

            try
            {
                // Patch the module
                var file = PEFile.FromFile(ModulePath);
                PEFileHelper helper = new(file);
                if (helper.Patch(methods, variables))
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
