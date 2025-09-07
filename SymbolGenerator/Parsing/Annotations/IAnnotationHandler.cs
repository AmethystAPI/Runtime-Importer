namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public interface IAnnotationHandler
    {
        object? Handle(RawAnnotation annotation);
    }
}
