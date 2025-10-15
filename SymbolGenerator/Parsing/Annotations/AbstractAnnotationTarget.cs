using Amethyst.Common.Utility;

namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public abstract class AbstractAnnotationTarget
    {
        public virtual CursorLocation? Location { get; set; }

        public HashSet<ProcessedAnnotation> Annotations { get; set; } = [];

        public bool HasAnnotation(AnnotationID id)
        {
            return Annotations.Any(a => a.ID == id);
        }

        public bool HasAnyOfAnnotations(IEnumerable<AnnotationID> ids)
        {
            foreach (var id in ids)
            {
                if (HasAnnotation(id))
                    return true;
            }
            return false;
        }
    }
}
