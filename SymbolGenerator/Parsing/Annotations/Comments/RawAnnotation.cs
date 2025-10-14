using Amethyst.Common.Utility;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.Comments
{
    public record RawAnnotation(string Tag, IEnumerable<string> Arguments, CursorLocation Location, AbstractAnnotationTarget Target)
    {
        public override string ToString()
        {
            return $"[{Tag}(...)]";
        }
    }
}
