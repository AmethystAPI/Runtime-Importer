using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Commands
{
    [Command("generate", Description = "Generates symbol files based on the provided configuration.")]
    public class GenerateCommand : ICommand
    {
        [CommandOption("compiler-args")]
        public string CompilerArguments { get; set; } = string.Empty;

        public ValueTask ExecuteAsync(IConsole console)
        {
            
            return default;
        }
    }
}
