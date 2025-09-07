namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AnnotationHandlerAttribute(string tag, string[] collidesWith, bool collidesWithSelf = true) : Attribute
    {
        public string HandlesTag { get; } = tag;
        public string[] CollidesWith { get; } = collidesWith;
        public bool CollidesWithSelf { get; } = collidesWithSelf;
    }
}
