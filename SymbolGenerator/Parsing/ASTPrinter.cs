using Amethyst.Common.Diagnostics;

namespace Amethyst.SymbolGenerator.Parsing
{
    public static class ASTPrinter
    {
        public static void PrintVariable(ASTVariable variable, string inputDir)
        {
            var kind = variable.IsFreeVariable ? "Free Variable" : "Variable";
            Logger.Debug($"{kind}: {variable.FullName}, Imported: {variable.IsImported}, Static: {variable.IsStatic}");
            if (variable.Location is not null && !string.IsNullOrEmpty(variable.Location.File))
                Logger.Debug($"   at {Path.GetRelativePath(inputDir, variable.Location.File)}:{variable.Location.Line}:{variable.Location.Column}");
        }

        public static void PrintMethod(ASTMethod method, string inputDir)
        {
            var kind = method.IsFreeFunction ? "Free Function" : "Method";
            Logger.Debug($"{kind}: {method.FullName}, Virtual: {method.IsVirtual}, Imported: {method.IsImported}");
            if (method.Location is not null && !string.IsNullOrEmpty(method.Location.File))
                Logger.Debug($"   at {Path.GetRelativePath(inputDir, method.Location.File)}:{method.Location.Line}:{method.Location.Column}");
        }

        public static void PrintClass(ASTClass cls, string inputDir)
        {
            // Build inheritance chain: Main : Base1 : Base2 ...
            var chain = cls.GetAncestors().Select(c => c.Class.Name).ToList();
            var inheritance = chain.Count > 0 ? $"{cls.Name} : {string.Join(" : ", chain)}" : cls.Name;
            Logger.Debug($"Class: {inheritance}");

            // Print full name and location
            if (!string.IsNullOrEmpty(cls.Namespace))
                Logger.Debug($"  FullName: {cls.FullName}");
            if (cls.Location != null)
                Logger.Debug($"  Location: {Path.GetRelativePath(inputDir, cls.Location.File)}:{cls.Location.Line}:{cls.Location.Column}");

            // Print methods
            foreach (var method in cls.Methods)
            {
                var kind = method.IsFreeFunction ? "Free Function" : "Method";
                Logger.Debug($"  {kind}: {method.Name}");
            }
        }
    }
}
