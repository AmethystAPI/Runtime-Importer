namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AnnotationHandlerAttribute(string tag, string[] aliases) : Attribute
    {
        public string[] Tags { get; } = [ 
            tag, 
            ..aliases 
        ];
    }
}
