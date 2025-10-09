using Amethyst.Common.Utility;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.ParameterPacks {
    public class VirtualPointerAnnotationParameterPack(RawAnnotation annotation) : AbstractParameterPack<VirtualPointerAnnotationParameterPack>(annotation) {
        public ulong Address { get; private set; } = 0;
        public string TargetVirtualTable { get; private set; } = string.Empty;
        public string? VirtualTableMangledName { get; set; } = null;
        public PlatformType Platform { get; private set; } = PlatformType.WinClient;

        public override VirtualPointerAnnotationParameterPack Parse() {
            if (Annotation.Target is not ASTClass cls)
                throw new UnhandledAnnotationException($"Virtual pointer annotation can only be applied to classes or structs. Applied to {annotation.Target.GetType().Name} instead.", annotation);

            string[] args = [.. Annotation.Arguments];
            if (args.Length < 1)
                throw new UnhandledAnnotationException($"Virtual pointer annotation requires at least one argument. Received {args.Length}.", Annotation);
            
            var addressArg = args[0];
            NumberStyles styles;
            if (args[0].StartsWith("0x")) {
                addressArg = addressArg.Replace("0x", "");
                styles = NumberStyles.HexNumber;
            }
            else
                styles = NumberStyles.Integer;
            if (!ulong.TryParse(addressArg, styles, null, out ulong addr))
                throw new UnhandledAnnotationException($"Virtual pointer annotation first argument must be a valid hexadecimal or decimal number. Received {args[0]}", Annotation);
            Address = addr;
            TargetVirtualTable = args.Length > 1 ? args[1] : "this";

            if (args.Length > 2)
                if (PlatformUtility.TryParse(args[2], out var platformType))
                    Platform = platformType;

            if (args.Length > 3)
                VirtualTableMangledName = args[3];
            return this;
        }
    }
}
