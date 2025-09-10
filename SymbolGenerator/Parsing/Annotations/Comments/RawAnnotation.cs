namespace Amethyst.SymbolGenerator.Parsing.Annotations.Comments
{
    public record RawAnnotation(string Tag, IEnumerable<string> Arguments, ASTCursorLocation Location)
    {
        public AbstractAnnotationTarget Target { get; set; } = null!;

        public override string ToString()
        {
            return $"[{Tag}(...)]";
        }
    }
}
