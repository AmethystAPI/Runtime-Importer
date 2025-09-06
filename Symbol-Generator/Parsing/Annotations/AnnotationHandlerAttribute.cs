using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AnnotationHandlerAttribute(string tag) : Attribute
    {
        public string HandlesTag { get; } = tag;
    }
}
