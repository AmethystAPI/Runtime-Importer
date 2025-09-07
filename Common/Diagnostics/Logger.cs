using System.Reflection;

namespace Amethyst.Common.Diagnostics
{
    public static class Logger
    {
        public static void WriteLine(object? message = null, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void Info(object? message) => 
            WriteLine($"{(
                Assembly.GetEntryAssembly()?.GetName()?.Name is { } name ? 
                $"[{name}] " :
                "")}[INFO] {message}", ConsoleColor.White);

        public static void Warn(string message) =>
            WriteLine($"{(
                Assembly.GetEntryAssembly()?.GetName()?.Name is { } name ?
                $"[{name}] " :
                "")}[WARN] {message}", ConsoleColor.Yellow);

        public static void Error(string message) =>
            WriteLine($"{(
                Assembly.GetEntryAssembly()?.GetName()?.Name is { } name ?
                $"[{name}] " :
                "")}[ERROR] {message}", ConsoleColor.Red);

        public static void Fatal(string message)
        {
            WriteLine($"{(
                Assembly.GetEntryAssembly()?.GetName()?.Name is { } name ?
                $"[{name}] " :
                "")}[FATAL] {message}", ConsoleColor.Magenta);
            Environment.Exit(1);
        }
    }
}
