using Amethyst.Common.Models;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.Handlers
{
    [AnnotationHandler("address", ["addr", "absolute", "abs", "at"])]
    public class AddressAnnotationHandler(AnnotationProcessor processor) : AbstractAnnotationHandler(processor)
    {
        public override void CanHandle(RawAnnotation annotation)
        {
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

            if (annotation.Target.HasAnyOfAnnotations([annotation.Tag, "signature"]))
                throw new UnhandledAnnotationException($"Multiple address or signature annotations applied to the same target {annotation.Target}.", annotation);
            
            string[] args = [.. annotation.Arguments];
            if (args.Length != 1)
                throw new UnhandledAnnotationException($"Address annotation requires exactly one argument. Received {args.Length}", annotation);
            
            if (!ulong.TryParse(args[0].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out _))
                throw new UnhandledAnnotationException($"Address annotation argument must be a valid hexadecimal number. Received {args[0]}", annotation);
        }

        public override ProcessedAnnotation Handle(RawAnnotation annotation)
        {
            string[] args = [.. annotation.Arguments];
            if (annotation.Target is ASTVariable variable)
            {
                return new ProcessedAnnotation(
                    annotation,
                    new VariableSymbolModel
                    {
                        Name = variable.MangledName,
                        Address = args[0]
                    }
                );
            }
            else if (annotation.Target is ASTMethod method)
            {
                return new ProcessedAnnotation(
                    annotation,
                    new FunctionSymbolModel
                    {
                        Name = method.MangledName,
                        Address = args[0]
                    }
                );
            }
            throw new UnhandledAnnotationException("THIS SHOULDN'T BE HAPPENING", annotation);
        }
    }
}
