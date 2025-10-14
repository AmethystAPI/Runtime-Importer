using Amethyst.Common.Utility;
using Amethyst.SymbolGenerator.Parsing.Annotations;

namespace Amethyst.SymbolGenerator.Parsing
{
    public class ASTClass : AbstractAnnotationTarget
    {
        public string? Name { get; set; }
        public string? Namespace { get; set; }
        public override CursorLocation? Location { get; set; }
        public ASTBaseSpecifier[] DirectBaseClasses { get; set; } = [];
        public ASTMethod[] Methods { get; set; } = [];
        public ASTVariable[] Variables { get; set; } = [];
        public string? RawComment { get; set; }
        public string FullName => string.IsNullOrEmpty(Namespace) ? Name! : $"{Namespace}::{Name}";
        public bool IsBase => DirectBaseClasses.Length == 0;

        public bool OwnsAtLeastOneVirtualMethod()
        {
            return Methods.Any(m => m.IsVirtual);
        }

        public bool HasAtLeastOneVirtualMethod()
        {
            if (OwnsAtLeastOneVirtualMethod())
                return true;
            foreach (var ancestor in GetAncestors())
            {
                if (ancestor.Class.HasAtLeastOneVirtualMethod())
                    return true;
            }
            return false;
        }

        public IEnumerable<ASTBaseSpecifier> GetAncestors()
        {
            foreach (var baseClass in DirectBaseClasses)
            {
                yield return baseClass;
                foreach (var ancestor in baseClass.Class.GetAncestors())
                {
                    yield return ancestor;
                }
            }
        }

        public int DistinctPolymorphicAncestorCount()
        {
            HashSet<ASTClass> distinctAncestors = new();
            foreach (var ancestor in GetAncestors())
            {
                if (ancestor.Class.OwnsAtLeastOneVirtualMethod())
                    distinctAncestors.Add(ancestor.Class);
            }
            return distinctAncestors.Count;
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}
