using Amethyst.Common.Utility;
using Amethyst.SymbolGenerator.Parsing.Annotations;

namespace Amethyst.SymbolGenerator.Parsing
{
    public class ASTVariable : AbstractAnnotationTarget
    {
        public string Name { get; set; } = string.Empty;
        public string MangledName { get; set; } = string.Empty;
        public string? Namespace { get; set; } = null;
        public ASTClass? DeclaringClass { get; set; } = null;
        public override CursorLocation? Location { get; set; } = null;
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

        public override string ToString()
        {
            return FullName;
        }
    }
}
