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

namespace Amethyst.SymbolGenerator.Commands
{
    [Command("generate", Description = "Generates symbol files based on the provided configuration.")]
    public class GenerateCommand : ICommand
    {
        [CommandOption("input", 'i', Description = "Path to the input directory containing header files.", IsRequired = true)]
        public DirectoryInfo Input { get; set; } = null!;

        [CommandOption("output", 'o', Description = "Path to the output directory where symbol files will be generated.", IsRequired = true)]
        public DirectoryInfo Output { get; set; } = null!;

        [CommandOption("compiler-args")]
        public string CompilerArguments { get; set; } = string.Empty;

        public ValueTask ExecuteAsync(IConsole console)
        {
            // Track changes in header files
            FileTracker headerTracker = new(
                inputDirectory: Input,
                checksumFile: new FileInfo(Path.Combine(Output.FullName, "file_checksums.json")),
                searchPatterns: ["*.h", "*.hpp", "*.hh", "*.hxx"]
            );
            IEnumerable<FileChange> changes = headerTracker.TrackChanges();
            

            return default;
        }
    }
}
