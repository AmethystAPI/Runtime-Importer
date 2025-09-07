namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public record RawAnnotation(string Tag, IEnumerable<string> Arguments, ASTCursorLocation Location)
    {
        public IAnnotationTarget Target { get; set; } = null!;

        public override string ToString()
        {
            return $"[{Tag}(...)]";
        }
    }
}
