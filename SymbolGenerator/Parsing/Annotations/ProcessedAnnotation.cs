using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using Newtonsoft.Json;

namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public record ProcessedAnnotation(RawAnnotation Annotation, object Data, Action<IEnumerable<ProcessedAnnotation>, ProcessedAnnotation, AnnotationProcessor>? ResolveReferences = null)
    {
        public AbstractAnnotationTarget Target => Annotation.Target;
        public bool Resolved { get; private set; } = false;

        public override string ToString()
        {
            return $"{Annotation} => {JsonConvert.SerializeObject(Data)}";
        }

        public void Resolve(AnnotationProcessor processor)
        {
            if (Resolved)
                return;
            ResolveReferences?.Invoke(processor.ProcessedAnnotations, this, processor);
            Resolved = true;
        }
    }
}
