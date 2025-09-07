using Amethyst.Common.Diagnostics;
using CliFx;
using System.Reflection;

namespace Amethyst.LibraryGenerator
{
    public static class Program
    {
        static async Task<int> Main(string[] args)
        {
            Version version = Assembly.GetEntryAssembly()?.GetName()?.Version ?? new Version(1, 0, 0);
            string shortVersion = $"{version.Major}.{version.Minor}.{version.Build}";
            string name = Assembly.GetEntryAssembly()?.GetName()?.Name ?? "Amethyst.LibraryGenerator";
            Logger.Info($"Starting '{name}' v{shortVersion}...");
            Logger.Info($"Created by ryd3v for Amethyst.");
            Logger.Info($"Repository: 'https://github.com/AmethystAPI/Symbol-Generator'.");

            return await new CliApplicationBuilder()
                .AddCommandsFromThisAssembly()
                .Build()
                .RunAsync(args);
        }
    }
}