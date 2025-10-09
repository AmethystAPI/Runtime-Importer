using Amethyst.Common.Utility;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.ParameterPacks {
    public class AddressAnnotationParameterPack(RawAnnotation annotation) : AbstractParameterPack<AddressAnnotationParameterPack>(annotation) {
        public ulong Address { get; private set; }
        public PlatformType Platform { get; private set; } = PlatformType.WinClient;

        public override AddressAnnotationParameterPack Parse() {
            string[] args = [..Annotation.Arguments];
            if (args.Length < 1)
                throw new UnhandledAnnotationException("Address annotation requires at least one argument.", Annotation);
            if (!ulong.TryParse(args[0].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out ulong address))
                throw new UnhandledAnnotationException($"Address annotation first argument must be a valid hexadecimal number. Received {args[0]}", Annotation);
            Address = address;
            if (args.Length > 1)
                if (PlatformUtility.TryParse(args[1], out var platformType))
                    Platform = platformType;
            return this;
        }
    }
}
