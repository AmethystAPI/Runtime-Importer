using Amethyst.Common.Models;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.Handlers
{
    [AnnotationHandler("address", ["signature"])]
    public class AddressAnnotationHandler : IAnnotationHandler
    {
        public bool CanApply(RawAnnotation annotation)
        {
            var args = annotation.Arguments.ToArray();
            if (!annotation.Target.IsMethod & !annotation.Target.IsVariable)
                throw new ArgumentException($"Address annotation can only be applied to methods or variables. Applied to {annotation.Target.GetType().Name} instead.");
            if (annotation.Target.Method is null & annotation.Target.Variable is null)
                throw new ArgumentException("Annotation target method or variable is null.");
            if (!annotation.Target.IsImported)
                throw new ArgumentException("Address annotation can only be applied to imported methods or variables.");
            if (annotation.Target.IsMethod && annotation.Target.Method!.HasBody)
                throw new ArgumentException("Address annotation can only be applied to body-less methods.");
            if (annotation.Target.IsVariable && annotation.Target.Variable!.DeclaringClass is not null && !annotation.Target.Variable.IsStatic)
                throw new ArgumentException("Address annotation can only be applied to static or global variables.");
            if (args.Length != 1)
                throw new ArgumentException($"Address annotation requires exactly one argument. Received {args.Length}");
            return true;
        }

        public object? Handle(RawAnnotation annotation)
        {
            var args = annotation.Arguments.ToArray();
            if (!ulong.TryParse(args[0].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out var address))
                throw new ArgumentException($"Address annotation argument must be a valid hexadecimal number. Received {args[0]}");
            if (annotation.Target.IsMethod && annotation.Target.Method is not null)
            {
                return new MethodSymbolJSONModel
                {
                    Name = annotation.Target.Method.MangledName,
                    Address = $"0x{address:X}"
                };
            }
            else if (annotation.Target.IsVariable && annotation.Target.Variable is not null)
            {
                return new VariableSymbolJSONModel
                {
                    Name = annotation.Target.Variable.MangledName,
                    Address = $"0x{address:X}"
                };
            }
            return null; // This should never be reached due to CanApply checks
        }
    }
}
