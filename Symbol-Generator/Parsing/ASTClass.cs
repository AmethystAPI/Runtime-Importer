using Amethyst.SymbolGenerator.Parsing.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing
{
    public class ASTClass : IAnnotationTarget
    {
        public string? Name { get; set; }
        public string? Namespace { get; set; }
        public ASTCursorLocation? Location { get; set; }
        public ASTBaseSpecifier[] DirectBaseClasses { get; set; } = [];
        public ASTMethod[] Methods { get; set; } = [];

        public string FullName => string.IsNullOrEmpty(Namespace) ? Name! : $"{Namespace}::{Name}";

        public bool IsBase => DirectBaseClasses.Length == 0;

        public ASTClass? Class => this;

        public ASTMethod? Method => null;

        public bool IsClass => true;

        public bool IsMethod => false;

        public HashSet<string> HandledAnnotations { get; set; } = [];

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
                    yield return ancestor;
            }
        }

        
    }
}
