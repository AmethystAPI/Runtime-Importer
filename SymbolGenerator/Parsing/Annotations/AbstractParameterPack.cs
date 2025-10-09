using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations {
    public abstract class AbstractParameterPack<T>(RawAnnotation annotation) {
        public RawAnnotation Annotation { get; } = annotation;

        public abstract T Parse();
    }
}
