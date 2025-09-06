using Amethyst.SymbolGenerator.Models;
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
            if (!annotation.Target.IsMethod && !annotation.Target.IsVariable)
                throw new ArgumentException($"Address annotation can only be applied to methods or static variables. Applied to {annotation.Target.GetType().Name} instead.");
            if (annotation.Target.Method is not null)
            {
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
                return new MethodSymbolJSONModel()
                {
                    Name = annotation.Target.Method.MangledName,
                    Address = $"0x{address:X}"
                };
            }
            else if (annotation.Target.Variable is not null)
            {
                if (!annotation.Target.Variable.IsImported)
                    throw new ArgumentException("Address annotation can only be applied to imported variables.");
                if (!annotation.Target.Variable.IsStatic)
                    throw new ArgumentException("Address annotation can only be applied to static variables.");
                if (annotation.Target.Variable.HasDefinition)
                    throw new ArgumentException("Address annotation can only be applied to variables that have don't have definition.");
                if (args.Length != 1)
                    throw new ArgumentException($"Address annotation requires exactly one argument. Received {args.Length}");
                if (!ulong.TryParse(args[0].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out var address))
                    throw new ArgumentException($"Address annotation argument must be a valid hexadecimal number. Received {args[0]}");
                if (!annotation.Target.HandledAnnotations.Add(annotation.Tag))
                    throw new InvalidOperationException($"Annotation target already has an Address annotation.");
                return new Dictionary<string, string>
                {
                    { "type", "static_variable_address" },
                    { "variable", annotation.Target.Variable.MangledName },
                    { "address", $"0x{address:X}" }
                };
            }
            else
            {
                throw new ArgumentException("Address annotation target is neither a method nor a variable.");
            }
        }
    }
}
