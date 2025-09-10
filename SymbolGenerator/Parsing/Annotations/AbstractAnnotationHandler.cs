using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;

namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public abstract class AbstractAnnotationHandler(AnnotationProcessor processor)
    {
        public readonly AnnotationProcessor Processor = processor;

        public abstract void CanHandle(RawAnnotation annotation);
        public abstract ProcessedAnnotation Handle(RawAnnotation annotation);
    }
}
