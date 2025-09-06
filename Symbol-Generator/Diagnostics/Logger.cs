using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Diagnostics
{
    public static class Logger
    {
        public static void Info(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[SymbolGenerator] [INFO] {message}");
            Console.ResetColor();
        }

        public static void Warning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[SymbolGenerator] [WARNING] {message}");
            Console.ResetColor();
        }

        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[SymbolGenerator] [ERROR] {message}");
            Console.ResetColor();
        }
    }
}
