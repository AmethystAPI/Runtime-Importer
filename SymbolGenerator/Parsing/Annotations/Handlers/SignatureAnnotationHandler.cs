using Amethyst.Common.Models;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using Amethyst.SymbolGenerator.Parsing.Annotations.ParameterPacks;
using System.Text.RegularExpressions;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.Handlers
{
    [AnnotationHandler("signature", ["sig", "pattern"])]
    public partial class SignatureAnnotationHandler(AnnotationProcessor processor, RawAnnotation annotation) : AbstractAnnotationHandler(processor, annotation)
    {
        public SignatureAnnotationParameterPack ParameterPack { get; } = new SignatureAnnotationParameterPack(annotation).Parse();

        public override HandlerAction CanHandle(RawAnnotation annotation)
        {
            if (ParameterPack.Platform != Processor.PlatformType)
                return HandlerAction.SilentlySkip;

            if (annotation.Target is not ASTMethod method)
                throw new UnhandledAnnotationException($"Signature annotation can only be applied to methods. Applied to {annotation.Target.GetType().Name} instead.", annotation);
            if (!method.IsImported)
                throw new UnhandledAnnotationException("Signature annotation can only be applied to imported methods.", annotation);

            if (annotation.Target.HasAnyOfAnnotations([
                new(annotation.Tag, ParameterPack.Platform),
                new("address", ParameterPack.Platform)
            ]))
                throw new UnhandledAnnotationException($"Multiple signature or address annotations applied to the same target {annotation.Target}.", annotation);
            return HandlerAction.Handle;
        }

        public override ProcessedAnnotation Handle(RawAnnotation annotation)
        {
            ASTMethod target = (annotation.Target as ASTMethod)!;
            return new ProcessedAnnotation(
                annotation,
                new(annotation.Tag, ParameterPack.Platform),
                new FunctionSymbolModel
                {
                    Name = target.MangledName,
                    Signature = ParameterPack.Signature
                }
            );
        }
    }
}
