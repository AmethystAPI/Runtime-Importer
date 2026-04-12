using Amethyst.Common.Diagnostics;
using CliFx;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Amethyst.SymbolGenerator
{
    public static class Program
    {
        public static string[] CompilerArguments = [];

        /// <summary>
        /// Expands @file response file arguments. Each line in the file becomes a separate argument.
        /// </summary>
        private static IEnumerable<string> ExpandResponseFiles(IEnumerable<string> args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith('@') && File.Exists(arg[1..]))
                {
                    foreach (var line in File.ReadAllLines(arg[1..]))
                    {
                        string trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                            yield return trimmed;
                    }
                }
                else
                {
                    yield return arg;
                }
            }
        }

        static async Task<int> Main(string[] args)
        {
            Version version = Assembly.GetEntryAssembly()?.GetName()?.Version ?? new Version(1, 0, 0);
            string shortVersion = $"{version.Major}.{version.Minor}.{version.Build}";
            string name = Assembly.GetEntryAssembly()?.GetName()?.Name ?? "Amethyst.SymbolGenerator";
            Logger.Info($"Starting '{name}' v{shortVersion}...");
            Logger.Debug($"Created by ryd3v for Amethyst.");
            Logger.Debug($"Repository: 'https://github.com/AmethystAPI/Runtime-Importer'.");

            try
            {
                //NativeLibrary.Load($"{Environment.CurrentDirectory}/runtimes/win-x64/native/libclang.dll");
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
                CompilerArguments = [.. ExpandResponseFiles(args.Skip(sepIndex + 1))];
            }

            return await new CliApplicationBuilder()
                .AddCommandsFromThisAssembly()
                .Build()
                .RunAsync(before);
        }
    }
}
