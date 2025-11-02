using Amethyst.Common.Utility;

namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public abstract class AbstractAnnotationTarget
    {
        public virtual CursorLocation? Location { get; set; }
        public HashSet<ProcessedAnnotation> Annotations { get; set; } = [];
        public bool IsAnnotated => Annotations.Count > 0;
        public abstract bool IsImported { get; set; }
        public abstract string IdentificationName { get; }
        public abstract string? RawComment { get; set; }
        public HashSet<string> Aliases { get; set; } = [];

        public bool IsNamedAs(string name)
        {
            return Utils.CompareSymbolsWithThreshold(IdentificationName, name) || Aliases.Contains(name, new FuzzyStringComparer());
        }

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
