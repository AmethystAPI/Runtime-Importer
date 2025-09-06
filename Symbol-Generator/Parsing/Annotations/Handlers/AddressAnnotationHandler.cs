using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.Handlers
{
    [AnnotationHandler("Address")]
    public class AddressAnnotationHandler : IAnnotationHandler
    {
        public object? Handle(RawAnnotation annotation)
        {
            var args = annotation.Arguments.ToArray();
            if (!annotation.Target.IsMethod)
                throw new ArgumentException($"Address annotation can only be applied to methods. Applied to {annotation.Target.GetType().Name} instead.");
            if (annotation.Target.Method is null)
                throw new ArgumentException("Annotation target method is null.");
            if (!annotation.Target.Method.IsImported)
                throw new ArgumentException("Address annotation can only be applied to imported methods.");
            if (annotation.Target.Method.HasBody)
                throw new ArgumentException("Address annotation can only be applied to body-less methods.");
            if (args.Length != 1)
                throw new ArgumentException($"Address annotation requires exactly one argument. Received {args.Length}");
            if (!ulong.TryParse(args[0].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out var address))
                throw new ArgumentException($"Address annotation argument must be a valid hexadecimal number. Received {args[0]}");
            if (annotation.Target.HandledAnnotations.Contains("Signature"))
                throw new InvalidOperationException($"Address annotation can't be applied to a method that already has a Signature annotation.");
            if (!annotation.Target.HandledAnnotations.Add(annotation.Tag))
                throw new InvalidOperationException($"Annotation target already has an Address annotation.");
            return new Dictionary<string, string>
            {
                { "type", "function_address" },
                { "function", annotation.Target.Method.MangledName },
                { "address", $"0x{address:X}" }
            };
        }
    }
}
