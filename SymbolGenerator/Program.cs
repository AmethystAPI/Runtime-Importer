using Amethyst.Common.Diagnostics;
using CliFx;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Amethyst.SymbolGenerator
{
    public static class Program
    {
        public static string[] CompilerArguments = [];

        static async Task<int> Main(string[] args)
        {
            Version version = Assembly.GetEntryAssembly()?.GetName()?.Version ?? new Version(1, 0, 0);
            string shortVersion = $"{version.Major}.{version.Minor}.{version.Build}";
            string name = Assembly.GetEntryAssembly()?.GetName()?.Name ?? "Amethyst.SymbolGenerator";
            Logger.Info($"Starting '{name}' v{shortVersion}...");
            Logger.Info($"Created by ryd3v for Amethyst.");
            Logger.Info($"Repository: 'https://github.com/AmethystAPI/Symbol-Generator'.");

            try
            {
                NativeLibrary.Load("libclang");
            }
            catch
            {
                Logger.Fatal("Failed to load libclang. Did you forget to instal LLVM and point it's /bin folder to PATH?");
                return 1;
            }

            // Get compiler arguments after the "--" separator
            int sepIndex = Array.IndexOf(args, "--");
            string[] before = args;
            if (sepIndex >= 0)
            {
                before = [.. args.Take(sepIndex)];
                CompilerArguments = [.. args.Skip(sepIndex + 1)];
            }

            return await new CliApplicationBuilder()
                .AddCommandsFromThisAssembly()
                .Build()
                .RunAsync(before);
        }
    }
}
