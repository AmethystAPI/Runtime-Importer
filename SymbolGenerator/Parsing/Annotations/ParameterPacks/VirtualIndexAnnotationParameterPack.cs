using Amethyst.Common.Utility;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.ParameterPacks {
    public class VirtualIndexAnnotationParameterPack(RawAnnotation annotation) : AbstractParameterPack<VirtualIndexAnnotationParameterPack>(annotation) {
        public uint Index { get; private set; } = 0;
        public bool ShouldInherit { get; private set; } = false;
        public string TargetVirtualTable { get; private set; } = string.Empty;
        public PlatformType Platform { get; private set; } = PlatformType.WinClient;

        public override VirtualIndexAnnotationParameterPack Parse() {
            if (Annotation.Target is not ASTMethod method)
                throw new UnhandledAnnotationException($"Virtual index annotation can only be applied to methods. Applied to {Annotation.Target.GetType().Name} instead.", Annotation);

            if (!method.IsVirtual)
                throw new UnhandledAnnotationException("Virtual index annotation can only be applied to virtual methods.", Annotation);

            string[] args = [.. Annotation.Arguments];
            if (args.Length < 1)
                throw new UnhandledAnnotationException($"Virtual index annotation requires at least one argument. Received {args.Length}.", Annotation);
            var indexArg = args[0];
            if (indexArg == "inherit" || indexArg == "i") {
                ShouldInherit = true;
                Index = 0;
            }
            else {
                NumberStyles styles;
                if (args[0].StartsWith("0x")) {
                    indexArg = indexArg.Replace("0x", "");
                    styles = NumberStyles.HexNumber;
                }
                else
                    styles = NumberStyles.Integer;
                if (!uint.TryParse(indexArg, styles, null, out uint index))
                    throw new UnhandledAnnotationException($"Virtual index annotation first argument must be a valid hexadecimal or decimal number or \"inherit\"/\"i\". Received {args[0]}", Annotation);
                Index = index;
            }

            if (ShouldInherit && method.OverrideOf is null)
                throw new UnhandledAnnotationException("Virtual index annotation 'inherit' argument can only be used on methods that override a base method.", Annotation);
            TargetVirtualTable = args.Length > 1 ? args[1] : "this";
            if (args.Length > 2)
                if (PlatformUtility.TryParse(args[2], out var platformType))
                    Platform = platformType;
            return this;
        }
    }
}
