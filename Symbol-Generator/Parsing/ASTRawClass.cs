using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing
{
    public class ASTRawClass
    {
        public string? Name { get; set; }
        public string? Namespace { get; set; }
        public ASTCursorLocation? Location { get; set; }
        public ASTRawClass[] DirectBaseClasses { get; set; } = [];

        public string FullName => string.IsNullOrEmpty(Namespace) ? Name! : $"{Namespace}::{Name}";

        public IEnumerable<ASTRawClass> GetAncestors()
        {
            foreach (var baseClass in DirectBaseClasses)
            {
                yield return baseClass;
                foreach (var ancestor in baseClass.GetAncestors())
                    yield return ancestor;
            }
        }
    }
}
