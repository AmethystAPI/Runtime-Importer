namespace Amethyst.SymbolGenerator.Parsing.Annotations.Comments
{
    public record RawAnnotation(string Tag, IEnumerable<string> Arguments, ASTCursorLocation Location, AbstractAnnotationTarget Target)
    {
        public override string ToString()
        {
            return $"[{Tag}(...)]";
        }
    }
}
