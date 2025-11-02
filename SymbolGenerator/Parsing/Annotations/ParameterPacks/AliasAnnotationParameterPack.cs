using Amethyst.Common.Utility;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.ParameterPacks {
    public class AliasAnnotationParameterPack(RawAnnotation annotation) : AbstractParameterPack<AliasAnnotationParameterPack>(annotation) {
        public string Alias { get; private set; } = string.Empty;
        public PlatformType Platform { get; private set; } = PlatformType.WinClient;

        public override AliasAnnotationParameterPack Parse() {
            string[] args = [.. Annotation.Arguments];
            if (args.Length < 1)
                throw new UnhandledAnnotationException("Alias annotation requires at least one argument.", Annotation);
            if (string.IsNullOrWhiteSpace(args[0]))
                throw new UnhandledAnnotationException("Alias annotation first argument cannot be empty or whitespace.", Annotation);
            Alias = args[0];
            if (args.Length > 1)
                if (PlatformUtility.TryParse(args[1], out var platformType))
                    Platform = platformType;
            return this;
        }
    }
}
