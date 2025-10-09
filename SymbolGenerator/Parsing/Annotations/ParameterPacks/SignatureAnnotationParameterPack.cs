using Amethyst.Common.Utility;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.ParameterPacks {
    public partial class SignatureAnnotationParameterPack(RawAnnotation annotation) : AbstractParameterPack<SignatureAnnotationParameterPack>(annotation) {
        public string Signature { get; private set; } = string.Empty;
        public PlatformType Platform { get; private set; } = PlatformType.WinClient;

        public override SignatureAnnotationParameterPack Parse() {
            string[] args = [.. Annotation.Arguments];
            if (args.Length < 1)
                throw new UnhandledAnnotationException("Signature annotation requires at least one argument.", Annotation);
            if (!IDASignatureRegex().IsMatch(args[0]))
                throw new UnhandledAnnotationException($"Signature annotation first argument must be a valid IDA-style signature. Received {args[0]}", Annotation);
            Signature = args[0];
            if (args.Length > 1)
                if (PlatformUtility.TryParse(args[1], out var platformType))
                    Platform = platformType;
            return this;
        }

        [GeneratedRegex(@"^(?:[0-9A-Fa-f]{2}|\?)(?:\s+(?:[0-9A-Fa-f]{2}|\?))*$")]
        private static partial Regex IDASignatureRegex();
    }
}
