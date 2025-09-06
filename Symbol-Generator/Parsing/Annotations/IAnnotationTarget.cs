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
        ASTVariable? Variable { get; }
        bool IsClass { get; }
        bool IsMethod { get; }
        bool IsVariable { get; }
        ASTCursorLocation? Location { get; }
        HashSet<string> HandledAnnotations { get; set; }
    }
}
