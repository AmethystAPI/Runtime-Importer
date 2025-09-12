using Amethyst.Common.Models;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.Handlers
{
    [AnnotationHandler("vidx", ["virtualindex", "vindex"])]
    public class VirtualIndexAnnotationHandler(AnnotationProcessor processor) : AbstractAnnotationHandler(processor)
    {
        public override void CanHandle(RawAnnotation annotation)
        {
            if (annotation.Target is not ASTMethod method)
                throw new UnhandledAnnotationException($"Virtual index annotation can only be applied to methods. Applied to {annotation.Target.GetType().Name} instead.", annotation);

            if (!method.IsVirtual)
                throw new UnhandledAnnotationException("Virtual index annotation can only be applied to virtual methods.", annotation);

            if (!method.IsImported)
                throw new UnhandledAnnotationException("Virtual index annotation can only be applied to imported methods.", annotation);

            if (annotation.Target.HasAnyOfAnnotations([annotation.Tag, "address", "signature"]))
                throw new UnhandledAnnotationException($"Multiple virtual index, address or signature annotations applied to the same target {annotation.Target}.", annotation);

            string[] args = [.. annotation.Arguments];
            if (args.Length < 1 || args.Length >= 3)
                throw new UnhandledAnnotationException($"Virtual index annotation requires one or two arguments. Received {args.Length}", annotation);

            bool inherit = args[0] == "inherit";
            if (inherit && method.OverrideOf is null)
                throw new UnhandledAnnotationException("Virtual index annotation 'inherit' argument can only be used on methods that override a base method.", annotation);

            if (!inherit && !uint.TryParse(args[0], out _))
                throw new UnhandledAnnotationException($"Virtual index annotation first argument must be a unsigned integer. Received {args[0]}", annotation);
        }

        public override ProcessedAnnotation Handle(RawAnnotation annotation)
        {
            ASTMethod target = (annotation.Target as ASTMethod)!;
            string[] args = [.. annotation.Arguments];
            bool inherit = args[0] == "inherit";
            string vtableName = args.Length > 1 ? args[1] : "this";
            return new ProcessedAnnotation(
                annotation,
                new VirtualFunctionSymbolModel
                {
                    Name = target.MangledName,
                    Index = inherit ? 0 : uint.Parse(args[0]),
                    VirtualTable = $"{target.DeclaringClass!.FullName}::vtable::'{vtableName}'",
                    Inherit = inherit,
                    Overrides = inherit ? target.OverrideOf!.MangledName : null
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
