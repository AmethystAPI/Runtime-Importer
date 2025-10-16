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

            if (annotation.Target is ASTMethod method) {
                if (!method.IsImported)
                    throw new UnhandledAnnotationException("Signature annotation can only be applied to imported methods.", annotation);
            }
            else if (annotation.Target is ASTVariable variable) {
                if (!variable.IsFreeVariable && !variable.IsStatic)
                    throw new UnhandledAnnotationException("Signature annotation can only be applied to static or free variables.", annotation);
                if (!variable.IsImported)
                    throw new UnhandledAnnotationException("Signature annotation can only be applied to imported variables.", annotation);
            }
            else
                throw new UnhandledAnnotationException($"Signature annotation can only be applied to methods or variables. Applied to {annotation.Target.GetType().Name} instead.", annotation);

            if (annotation.Target.HasAnyOfAnnotations([
                new(annotation.Tag, ParameterPack.Platform),
                new("address", ParameterPack.Platform)
            ]))
                throw new UnhandledAnnotationException($"Multiple signature or address annotations applied to the same target {annotation.Target}.", annotation);
            return HandlerAction.Handle;
        }

        public override ProcessedAnnotation Handle(RawAnnotation annotation)
        {
            if (annotation.Target is ASTVariable variable)
            {
                return new ProcessedAnnotation(
                    annotation,
                    new(annotation.Tag, ParameterPack.Platform),
                    new VariableSymbolModel {
                        Name = variable.MangledName,
                        Signature = ParameterPack.Signature
                    }
                );
            }
            else if (annotation.Target is ASTMethod method)
            {
                return new ProcessedAnnotation(
                    annotation,
                    new(annotation.Tag, ParameterPack.Platform),
                    new FunctionSymbolModel {
                        Name = method.MangledName,
                        Signature = ParameterPack.Signature
                    }
                );
            }
            throw new UnhandledAnnotationException($"Signature annotation can only be applied to methods or variables. Applied to {annotation.Target.GetType().Name} instead.", annotation);
        }
    }
}
