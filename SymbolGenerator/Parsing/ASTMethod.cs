using Amethyst.SymbolGenerator.Parsing.Annotations;

namespace Amethyst.SymbolGenerator.Parsing
{
    public class ASTMethod : AbstractAnnotationTarget
    {
        public string Name { get; set; } = null!;
        public string MangledName { get; set; } = null!;
        public string? Namespace { get; set; } = null;
        public ASTClass? DeclaringClass { get; set; } = null;
        public override ASTCursorLocation? Location { get; set; } = null;
        public bool IsVirtual { get; set; } = false;
        public bool IsImported { get; set; } = false;
        public bool IsConstructor { get; set; } = false;
        public bool IsDestructor { get; set; } = false;
        public string? RawComment { get; set; } = null;
        public ASTMethod? OverrideOf { get; set; }
        public bool IsFreeFunction => DeclaringClass is null;
        public string FullName
        {
            get
            {
                if (DeclaringClass is not null)
                {
                    return $"{DeclaringClass.FullName}::{Name}";
                }
                else
                {
                    return string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}::{Name}";
                }
            }
        }

        public static List<ASTMethod> SortNonOverridesFirst(IEnumerable<ASTMethod> methods)
        {
            var sorted = new List<ASTMethod>();
            var visited = new HashSet<ASTMethod>();
            void Visit(ASTMethod method)
            {
                if (visited.Contains(method))
                    return;
                if (method.OverrideOf is not null)
                    Visit(method.OverrideOf);
                visited.Add(method);
                sorted.Add(method);
            }
            foreach (var method in methods)
            {
                Visit(method);
            }
            return sorted;
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}
