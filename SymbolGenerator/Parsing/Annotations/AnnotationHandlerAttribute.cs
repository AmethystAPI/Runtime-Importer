namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AnnotationHandlerAttribute(string tag) : Attribute
    {
        public string HandlesTag { get; } = tag;
    }
}
