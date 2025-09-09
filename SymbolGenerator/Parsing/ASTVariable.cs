using Amethyst.SymbolGenerator.Parsing.Annotations;

namespace Amethyst.SymbolGenerator.Parsing
{
    public class ASTVariable : IAnnotationTarget
    {
        public string Name { get; set; } = string.Empty;
        public string MangledName { get; set; } = string.Empty;
        public string? Namespace { get; set; } = null;
        public ASTClass? DeclaringClass { get; set; } = null;
        public ASTCursorLocation? Location { get; set; } = null;
        public bool IsImported { get; set; } = false;
        public string? RawComment { get; set; } = null;
        public bool IsStatic { get; set; } = false;

        public bool IsFreeVariable => DeclaringClass is null;

        public string FullName
        {
            get
            {
                if (DeclaringClass is not null)
                {
                    return $"{DeclaringClass.FullName}::{Name}";
                }
                else
                {
                    return string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}::{Name}";
                }
            }
        }

        public ASTClass? Class => DeclaringClass;

        public ASTMethod? Method => null;

        public ASTVariable? Variable => this;

        public bool IsClass => false;

        public bool IsMethod => false;

        public bool IsVariable => true;

        public HashSet<string> HandledAnnotations { get; set; } = [];
    }
}
