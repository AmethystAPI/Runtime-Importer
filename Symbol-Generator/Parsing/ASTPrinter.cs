using Amethyst.SymbolGenerator.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing
{
    public static class ASTPrinter
    {
        public static void PrintMethod(ASTMethod method, string inputDir)
        {
            var kind = method.IsFreeFunction ? "Free Function" : "Method";
            Logger.Info($"{kind}: {method.FullName}, Virtual: {method.IsVirtual}, Imported: {method.IsImported}, HasBody: {method.HasBody}");
            if (method.Location is not null && !string.IsNullOrEmpty(method.Location.File))
                Logger.Info($"   at {Path.GetRelativePath(inputDir, method.Location.File)}:{method.Location.Line}:{method.Location.Column}:{method.Location.Offset}");
        }

        public static void PrintClass(ASTClass cls, string inputDir)
        {
            // Build inheritance chain: Main : Base1 : Base2 ...
            var chain = cls.GetAncestors().Select(c => c.Class.Name).ToList();
            var inheritance = chain.Count > 0 ? $"{cls.Name} : {string.Join(" : ", chain)}" : cls.Name;
            Logger.Info($"Class: {inheritance}");

            // Print full name and location
            if (!string.IsNullOrEmpty(cls.Namespace))
                Logger.Info($"  FullName: {cls.FullName}");
            if (cls.Location != null)
                Logger.Info($"  Location: {Path.GetRelativePath(inputDir, cls.Location.File)}:{cls.Location.Line}:{cls.Location.Column}:{cls.Location.Offset}");

            // Print methods
            foreach (var method in cls.Methods)
            {
                var kind = method.IsFreeFunction ? "Free Function" : "Method";
                Logger.Info($"  {kind}: {method.Name}");
            }
        }
    }
}
