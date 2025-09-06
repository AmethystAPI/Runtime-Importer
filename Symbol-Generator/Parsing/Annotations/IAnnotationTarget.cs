using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public interface IAnnotationTarget
    {
        string? Name { get; }
        string? FullName { get; }
        ASTClass? Class { get; }
        ASTMethod? Method { get; }
        bool IsClass { get; }
        bool IsMethod { get; }
        ASTCursorLocation? Location { get; }
        HashSet<string> HandledAnnotations { get; set; }
    }
}
