using Amethyst.Common.Models;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.Handlers
{
    [AnnotationHandler("vtable", ["virtualpointer", "vtableptr", "vtablepointer", "vptr"])]
    public class VirtualPointerAnnotation(AnnotationProcessor processor) : AbstractAnnotationHandler(processor)
    {
        public override void CanHandle(RawAnnotation annotation)
        {
            if (annotation.Target is not ASTClass cls)
                throw new UnhandledAnnotationException($"Virtual table annotation can only be applied to classes. Applied to {annotation.Target.GetType().Name} instead.", annotation);

            string[] args = [.. annotation.Arguments];
            if (args.Length < 1 || args.Length >= 4)
                throw new UnhandledAnnotationException($"Virtual table annotation requires exactly one, two or three arguments. Received {args.Length}", annotation);

            var targetAnnotations = annotation.Target.Annotations.Where(a => AnnotationProcessor.GetOfficialTagForAlias(a.Annotation.Tag) == "vtable");
            foreach (var existing in targetAnnotations)
            {
                if (existing.Annotation.Arguments.ElementAt(1) == args[1])
                    throw new UnhandledAnnotationException($"Multiple virtual table annotations with the same label '{args[1]}' applied to the same target {annotation.Target}.", annotation);
            }

            if (!ulong.TryParse(args[0].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out _))
                throw new UnhandledAnnotationException($"Virtual table annotation first argument must be a valid hexadecimal number. Received {args[0]}", annotation);
        }

        public override ProcessedAnnotation Handle(RawAnnotation annotation)
        {
            ASTClass target = (annotation.Target as ASTClass)!;
            string[] args = [.. annotation.Arguments];
            string label = args.Length > 1 ? args[1] : "this";
            string? vtableMangledLabel = args.Length > 2 ? args[2] : null;
            return new ProcessedAnnotation(
                annotation,
                new VirtualTableSymbolModel
                {
                    Name = $"{target.FullName}::vtable::'{label}'",
                    Address = args[0],
                    ForWhat = label,
                    VtableMangledLabel = vtableMangledLabel
                }
            );
        }
    }
}
