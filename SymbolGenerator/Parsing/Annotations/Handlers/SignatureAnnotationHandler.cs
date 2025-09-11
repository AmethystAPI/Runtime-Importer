using Amethyst.Common.Models;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using System.Text.RegularExpressions;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.Handlers
{
    [AnnotationHandler("signature", ["sig", "pattern"])]
    public partial class SignatureAnnotationHandler(AnnotationProcessor processor) : AbstractAnnotationHandler(processor)
    {
        public override void CanHandle(RawAnnotation annotation)
        {
            if (annotation.Target is not ASTMethod method)
                throw new UnhandledAnnotationException($"Signature annotation can only be applied to methods. Applied to {annotation.Target.GetType().Name} instead.", annotation);
            
            if (annotation.Target.HasAnyOfAnnotations([annotation.Tag, "address"]))
                throw new UnhandledAnnotationException($"Multiple signature or address annotations applied to the same target {annotation.Target}.", annotation);
            
            if (!method.IsImported)
                throw new UnhandledAnnotationException("Signature annotation can only be applied to imported methods.", annotation);
            
            string[] args = [.. annotation.Arguments];
            if (args.Length != 1)
                throw new UnhandledAnnotationException($"Signature annotation requires exactly one argument. Received {args.Length}", annotation);
            
            if (!IDASignatureRegex().IsMatch(args[0]))
                throw new UnhandledAnnotationException($"Signature annotation argument must be a valid IDA-style signature. Received {args[0]}", annotation);
        }

        public override ProcessedAnnotation Handle(RawAnnotation annotation)
        {
            ASTMethod target = (annotation.Target as ASTMethod)!;
            string[] args = [.. annotation.Arguments];
            return new ProcessedAnnotation(
                annotation,
                new FunctionSymbolModel
                {
                    Name = target.MangledName,
                    Signature = args[0]
                }
            );
        }

        [GeneratedRegex(@"^(?:[0-9A-Fa-f]{2}|\?)(?:\s+(?:[0-9A-Fa-f]{2}|\?))*$")]
        private static partial Regex IDASignatureRegex();
    }
}
