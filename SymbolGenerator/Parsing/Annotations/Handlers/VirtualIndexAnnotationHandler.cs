using Amethyst.Common.Models;
using Amethyst.Common.Utility;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using Amethyst.SymbolGenerator.Parsing.Annotations.ParameterPacks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.Handlers
{
    [AnnotationHandler("vidx", ["virtualindex", "vindex"])]
    public class VirtualIndexAnnotationHandler(AnnotationProcessor processor, RawAnnotation annotation) : AbstractAnnotationHandler(processor, annotation)
    {
        public VirtualIndexAnnotationParameterPack ParameterPack { get; } = new VirtualIndexAnnotationParameterPack(annotation).Parse();

        public override HandlerAction CanHandle(RawAnnotation annotation)
        {
            if (ParameterPack.Platform != processor.PlatformType)
                return HandlerAction.SilentlySkip;

            // Validated by the Parameter Pack
            ASTMethod method = (annotation.Target as ASTMethod)!;

            if (!method.IsImported)
                throw new UnhandledAnnotationException("Virtual index annotation can only be applied to imported methods.", annotation);

            if (annotation.Target.HasAnyOfAnnotations([
                new(annotation.Tag, ParameterPack.Platform),
                new("address", ParameterPack.Platform),
                new("signature", ParameterPack.Platform)
            ]))
                throw new UnhandledAnnotationException($"Multiple virtual index, address or signature annotations applied to the same target {annotation.Target}.", annotation);
            return HandlerAction.Handle;
        }

        public override ProcessedAnnotation Handle(RawAnnotation annotation)
        {
            // Validated by the Parameter Pack
            ASTMethod target = (annotation.Target as ASTMethod)!;
            return new ProcessedAnnotation(
                annotation,
                new(annotation.Tag, ParameterPack.Platform),
                new VirtualFunctionSymbolModel
                {
                    Name = target.MangledName,
                    Index = ParameterPack.Index,
                    VirtualTable = $"{target.DeclaringClass!.FullName}::vtable::'{ParameterPack.TargetVirtualTable}'",
                    Inherit = ParameterPack.ShouldInherit,
                    Overrides = ParameterPack.ShouldInherit ? target.OverrideOf!.MangledName : null
                },
                Resolve
            );
        }

        private static void Resolve(IEnumerable<ProcessedAnnotation> annotations, ProcessedAnnotation annotation, AnnotationProcessor processor)
        {
            var otherVirtualIndexAnnotations = annotations
                .Where(a => a.Data is VirtualFunctionSymbolModel)
                .Select(a => (a.Data as VirtualFunctionSymbolModel)!);

            var model = (annotation.Data as VirtualFunctionSymbolModel)!;
            if (model.Inherit)
            {
                var baseMethodVidx = otherVirtualIndexAnnotations
                    .FirstOrDefault(a => a.Name == model.Overrides) ?? 
                    throw new UnhandledAnnotationException($"Could not resolve inherited virtual index for method '{model.Name}'. No virtual index annotation found for overridden method '{model.Overrides}'.", annotation.Annotation);
                model.Index = baseMethodVidx.Index;
            }
        }
    }
}
