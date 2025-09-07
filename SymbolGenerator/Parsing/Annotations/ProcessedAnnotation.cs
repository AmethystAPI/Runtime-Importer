using Newtonsoft.Json;

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
