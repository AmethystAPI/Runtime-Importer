namespace Amethyst.SymbolGenerator.Parsing
{
    public record ASTCursorLocation(string File, uint Line, uint Column, uint Offset)
    {
        override public string ToString()
        {
            return $"{File}:{Line}:{Column}:{Offset}";
        }
    }
}
