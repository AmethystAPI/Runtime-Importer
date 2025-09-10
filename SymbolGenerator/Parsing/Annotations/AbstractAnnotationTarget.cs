namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public abstract class AbstractAnnotationTarget
    {
        public virtual ASTCursorLocation? Location { get; set; }

        public HashSet<ProcessedAnnotation> Annotations { get; set; } = [];

        public bool HasAnnotation(string tag)
        {
            string officialTag = AnnotationProcessor.GetOfficialTagForAlias(tag);
            return Annotations.Any(a => a.Annotation.Tag.Equals(officialTag, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasAnyOfAnnotations(IEnumerable<string> tags)
        {
            foreach (var tag in tags)
            {
                if (HasAnnotation(tag))
                    return true;
            }
            return false;
        }
    }
}
