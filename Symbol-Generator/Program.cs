using Amethyst.SymbolGenerator.Diagnostics;
using CliFx;
using System.Runtime.InteropServices;

namespace Amethyst.SymbolGenerator
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                NativeLibrary.Load("libclang");
            }
            catch
            {
                Logger.Error("Failed to load libclang. Did you forget to instal LLVM and point it's /bin folder to PATH?");
                return 1;
            }

            return await new CliApplicationBuilder()
                .AddCommandsFromThisAssembly()
                .Build()
                .RunAsync(args);
        }
    }
}
