using Amethyst.Common.Diagnostics;
using CliFx;
using System.Reflection;

namespace Amethyst.ModuleTweaker
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            Version version = Assembly.GetEntryAssembly()?.GetName()?.Version ?? new Version(1, 0, 0);
            string shortVersion = $"{version.Major}.{version.Minor}.{version.Build}";
            string name = Assembly.GetEntryAssembly()?.GetName()?.Name ?? "Amethyst.ModuleTweaker";
            Logger.Debug($"Starting '{name}' v{shortVersion}...");
            Logger.Debug($"Created by ryd3v for Amethyst.");
            Logger.Debug($"Repository: 'https://github.com/AmethystAPI/Runtime-Importer'.");

            return await new CliApplicationBuilder()
                .AddCommandsFromThisAssembly()
                .Build()
                .RunAsync(args);
        }
    }
}
