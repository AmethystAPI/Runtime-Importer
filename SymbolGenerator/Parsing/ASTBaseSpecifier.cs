namespace Amethyst.SymbolGenerator.Parsing
{
    public class ASTBaseSpecifier
    {
        public ASTClass Class { get; set; } = null!;
        public bool IsVirtualBase { get; set; } = false;
    }
}
