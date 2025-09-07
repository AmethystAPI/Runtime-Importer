namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public interface IAnnotationHandler
    {
        bool CanApply(RawAnnotation annotation);
        object? Handle(RawAnnotation annotation);
    }
}
