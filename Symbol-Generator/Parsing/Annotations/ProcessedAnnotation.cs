using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public record ProcessedAnnotation(RawAnnotation Annotation, object Data)
    {
        public IAnnotationTarget Target => Annotation.Target;

        public override string ToString()
        {
            return $"{Annotation} => {JsonConvert.SerializeObject(Data)}";
        }
    }
}
