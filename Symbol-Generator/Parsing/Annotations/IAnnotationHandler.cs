using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public interface IAnnotationHandler
    {
        object? Handle(RawAnnotation annotation);
    }
}
