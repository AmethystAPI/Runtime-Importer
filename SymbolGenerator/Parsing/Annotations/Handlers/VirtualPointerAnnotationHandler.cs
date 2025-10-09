using Amethyst.Common.Models;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using Amethyst.SymbolGenerator.Parsing.Annotations.ParameterPacks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.Handlers
{
    [AnnotationHandler("vtable", ["virtualpointer", "vtableptr", "vtablepointer", "vptr"])]
    public class VirtualPointerAnnotation(AnnotationProcessor processor, RawAnnotation annotation) : AbstractAnnotationHandler(processor, annotation)
    {
        public VirtualPointerAnnotationParameterPack ParameterPack { get; } = new VirtualPointerAnnotationParameterPack(annotation).Parse();

        public override HandlerAction CanHandle(RawAnnotation annotation)
        {
            if (ParameterPack.Platform != Processor.PlatformType)
                return HandlerAction.SilentlySkip;

            var targetAnnotations = annotation.Target.Annotations.Where(a => AnnotationProcessor.GetCanonicalTagForAlias(a.Annotation.Tag) == "vtable");
            foreach (var existing in targetAnnotations)
            {
                if (existing.ID.Platform != ParameterPack.Platform)
                    continue;
                VirtualPointerAnnotationParameterPack existingParameterPack = new VirtualPointerAnnotationParameterPack(existing.Annotation)
                    .Parse();

                if (existingParameterPack.TargetVirtualTable == ParameterPack.TargetVirtualTable)
                    throw new UnhandledAnnotationException($"Multiple virtual table annotations with the same label '{ParameterPack.TargetVirtualTable}' applied to the same target {annotation.Target}.", annotation);
            }

            return HandlerAction.Handle;
        }

        public override ProcessedAnnotation Handle(RawAnnotation annotation)
        {
            // Validated by the Parameter Pack
            ASTClass target = (annotation.Target as ASTClass)!;
            return new ProcessedAnnotation(
                annotation,
                new(annotation.Tag, ParameterPack.Platform),
                new VirtualTableSymbolModel
                {
                    Name = $"{target.FullName}::vtable::'{ParameterPack.TargetVirtualTable}'",
                    Address = $"0x{ParameterPack.Address:x}",
                    ForWhat = ParameterPack.TargetVirtualTable,
                    VtableMangledLabel = ParameterPack.VirtualTableMangledName
                }
            );
        }
    }
}
