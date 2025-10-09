using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;

namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public enum HandlerAction {
        Handle,
        SilentlySkip
    }

    public abstract class AbstractAnnotationHandler(AnnotationProcessor processor, RawAnnotation annotation)
    {
        public readonly AnnotationProcessor Processor = processor;
        public readonly RawAnnotation Annotation = annotation;

        public abstract HandlerAction CanHandle(RawAnnotation annotation);
        public abstract ProcessedAnnotation Handle(RawAnnotation annotation);
    }
}
