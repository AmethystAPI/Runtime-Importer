using Amethyst.SymbolGenerator.Diagnostics;
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
            Version version = Assembly.GetExecutingAssembly().GetName()?.Version ?? new Version(1, 0, 0);
            string shortVersion = $"{version.Major}.{version.Minor}.{version.Build}";
            Logger.Info($"Starting Symbol Generator v{shortVersion}...");
            Logger.Info("Created by ryd3v...");

            try
            {
                NativeLibrary.Load("libclang");
            }
            catch
            {
                Logger.Error("Failed to load libclang. Did you forget to instal LLVM and point it's /bin folder to PATH?");
                return 1;
            }

            int sepIndex = Array.IndexOf(args, "--");

            string[] before;
            string[] after;

            if (sepIndex >= 0)
            {
                before = args.Take(sepIndex).ToArray();
                after = args.Skip(sepIndex + 1).ToArray();
                CompilerArguments = after;
            }
            else
            {
                before = args;
                after = Array.Empty<string>();
            }

            return await new CliApplicationBuilder()
                .AddCommandsFromThisAssembly()
                .Build()
                .RunAsync(before);
        }
    }
}
