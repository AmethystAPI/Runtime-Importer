using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public record RawAnnotation(string Tag, IEnumerable<string> Arguments, ASTCursorLocation Location)
    {
        public IAnnotationTarget Target { get; set; } = null!;

        public override string ToString()
        {
            return $"[{Tag}({string.Join(", ", Arguments)})]";
        }
    }
}
