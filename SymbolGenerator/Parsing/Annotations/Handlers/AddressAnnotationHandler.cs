using Amethyst.Common.Models;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.Handlers
{
    [AnnotationHandler("address", ["signature"])]
    public class AddressAnnotationHandler : IAnnotationHandler
    {
        public bool CanApply(RawAnnotation annotation)
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
            return true;
        }

        public object? Handle(RawAnnotation annotation)
        {
            var args = annotation.Arguments.ToArray();
            if (!ulong.TryParse(args[0].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out var address))
                throw new ArgumentException($"Address annotation argument must be a valid hexadecimal number. Received {args[0]}");
            return new MethodSymbolJSONModel()
            {
                Name = annotation.Target.Method!.MangledName,
                Address = $"0x{address:X}"
            };
        }
    }
}
