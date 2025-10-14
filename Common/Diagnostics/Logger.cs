using Amethyst.Common.Utility;
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
        
        public static void Info(object? message, CursorLocation? location = null)
        {
            WriteLine($"{(location is not null && location.File != "<unknown>" ? location.ToString() + ": " : (Assembly.GetEntryAssembly()?.GetName().Name is not string name ? "Unknown: " : name.Trim() + ": "))} message: {message}", ConsoleColor.White);
        }

        public static void Debug(object? message, CursorLocation? location = null)
        {
#if DEBUG
            WriteLine($"{(location is not null && location.File != "<unknown>" ? location.ToString() + ": " : (Assembly.GetEntryAssembly()?.GetName().Name is not string name ? "Unknown: " : name.Trim() + ": "))}message: {message}", ConsoleColor.White);
#endif
        }

        public static void Warn(string message, CursorLocation? location = null) =>
            WriteLine($"{(location is not null && location.File != "<unknown>" ? location?.ToString() + ": " : (Assembly.GetEntryAssembly()?.GetName().Name is not string name ? "Unknown: " : name.Trim() + ": "))}warning: {message}", ConsoleColor.Yellow);

        public static void Error(string message, CursorLocation? location = null) =>
            WriteLine($"{(location is not null && location.File != "<unknown>" ? location?.ToString() + ": " : (Assembly.GetEntryAssembly()?.GetName().Name is not string name ? "Unknown: " : name.Trim() + ": "))}error: {message}", ConsoleColor.Red);

        public static void Fatal(string message, CursorLocation? location = null)
        {
            WriteLine($"{(location is not null && location.File != "<unknown>" ? location?.ToString() + ": " : (Assembly.GetEntryAssembly()?.GetName().Name is not string name ? "Unknown: " : name.Trim() + ": "))}fatal error: {message}", ConsoleColor.Magenta);
            Environment.Exit(1);
        }
    }
}
