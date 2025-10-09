using Amethyst.Common.Models;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using Amethyst.SymbolGenerator.Parsing.Annotations.ParameterPacks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.Handlers
{
    [AnnotationHandler("address", ["addr", "absolute", "abs", "at"])]
    public class AddressAnnotationHandler(AnnotationProcessor processor, RawAnnotation annotation) : AbstractAnnotationHandler(processor, annotation)
    {
        public AddressAnnotationParameterPack ParameterPack { get; } = new AddressAnnotationParameterPack(annotation).Parse();

        public override HandlerAction CanHandle(RawAnnotation annotation)
        {
            if (ParameterPack.Platform != Processor.PlatformType)
                return HandlerAction.SilentlySkip;

            if (annotation.Target is ASTMethod method)
            {
                if (!method.IsImported)
                    throw new UnhandledAnnotationException("Address annotation can only be applied to imported methods.", annotation);
            }
            else if (annotation.Target is ASTVariable variable)
            {
                if (!variable.IsImported)
                    throw new UnhandledAnnotationException("Address annotation can only be applied to imported variables.", annotation);
            }
            else
            {
                throw new UnhandledAnnotationException($"Address annotation can only be applied to methods or variables. Applied to {annotation.Target.GetType().Name} instead.", annotation);
            }

            if (annotation.Target.HasAnyOfAnnotations([
                new(annotation.Tag, ParameterPack.Platform), 
                new("signature", ParameterPack.Platform)
            ]))
                throw new UnhandledAnnotationException($"Multiple address or signature annotations applied to the same target {annotation.Target}.", annotation);
            return HandlerAction.Handle;
        }

        public override ProcessedAnnotation Handle(RawAnnotation annotation)
        {
            if (annotation.Target is ASTVariable variable)
            {
                return new ProcessedAnnotation(
                    annotation,
                    new(annotation.Tag, ParameterPack.Platform),
                    new VariableSymbolModel
                    {
                        Name = variable.MangledName,
                        Address = $"0x{ParameterPack.Address}"
                    }
                );
            }
            else if (annotation.Target is ASTMethod method)
            {
                return new ProcessedAnnotation(
                    annotation,
                    new(annotation.Tag, ParameterPack.Platform),
                    new FunctionSymbolModel
                    {
                        Name = method.MangledName,
                        Address = $"0x{ParameterPack.Address}"
                    }
                );
            }
            throw new UnhandledAnnotationException("THIS SHOULDN'T BE HAPPENING", annotation);
        }
    }
}
