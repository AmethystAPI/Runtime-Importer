using Amethyst.SymbolGenerator.Parsing.Annotations;

namespace Amethyst.SymbolGenerator.Parsing
{
    public class ASTMethod : IAnnotationTarget
    {
        public string Name { get; set; } = null!;
        public string MangledName { get; set; } = null!;
        public string? Namespace { get; set; } = null;
        public ASTClass? DeclaringClass { get; set; } = null;
        public ASTCursorLocation? Location { get; set; } = null;
        public bool IsVirtual { get; set; } = false;
        public bool IsImported { get; set; } = false;
        public bool HasBody { get; set; } = false;
        public bool IsDestructor { get; set; } = false;
        public string? RawComment { get; set; } = null;

        public bool IsFreeFunction => DeclaringClass is null;

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

        public ASTMethod? Method => this;

        public bool IsClass => false;

        public bool IsMethod => true;

        public HashSet<string> HandledAnnotations { get; set; } = [];

        public ASTVariable? Variable => null;

        public bool IsVariable => false;
    }
}
