using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public class UnhandledAnnotationException(string message, RawAnnotation annotation) : Exception(message)
    {
        public RawAnnotation Annotation { get; } = annotation;
    }
}
