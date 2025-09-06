using Amethyst.SymbolGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.Handlers
{
    [AnnotationHandler("Signature")]
    public partial class SignatureAnnotationHandler : IAnnotationHandler
    {
        public object? Handle(RawAnnotation annotation)
        {
            var args = annotation.Arguments.ToArray();
            if (!annotation.Target.IsMethod)
                throw new ArgumentException($"Signature annotation can only be applied to methods. Applied to {annotation.Target.GetType().Name} instead.");
            if (annotation.Target.Method is null)
                throw new ArgumentException("Annotation target method is null.");
            if (!annotation.Target.Method.IsImported)
                throw new ArgumentException("Signature annotation can only be applied to imported methods.");
            if (annotation.Target.Method.HasBody)
                throw new ArgumentException("Signature annotation can only be applied to body-less methods.");
            if (args.Length != 1)
                throw new ArgumentException($"Signature annotation requires exactly one argument. Received {args.Length}");
            if (string.IsNullOrWhiteSpace(args[0]))
                throw new ArgumentException("Signature annotation argument cannot be empty.");
            if (!IDASignatureRegex().Match(args[0]).Success)
                throw new ArgumentException($"Signature annotation argument must be a valid byte signature (e.g., '48 8B ? ? ? 00 00'). Received '{args[0]}'");
            if (annotation.Target.HandledAnnotations.Contains("Address"))
                throw new InvalidOperationException($"Signature annotation can't be applied to a method that already has a Address annotation.");
            if (!annotation.Target.HandledAnnotations.Add(annotation.Tag))
                throw new InvalidOperationException($"Annotation target already has a Signature annotation.");
            return new MethodSymbolJSONModel()
            {
                Name = annotation.Target.Method.MangledName,
                Signature = args[0]
            };
        }

        [GeneratedRegex(@"^(?:[0-9A-Fa-f]{2}|\?)(?:\s+(?:[0-9A-Fa-f]{2}|\?))*$")]
        private static partial Regex IDASignatureRegex();
    }
}
