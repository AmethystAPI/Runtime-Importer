using Amethyst.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations {
    public class AnnotationID(string tag, PlatformType platform) {
        public string Tag { get; } = tag;
        public PlatformType Platform { get; } = platform;

        public override int GetHashCode() {
            var canonicalTag = AnnotationProcessor.GetCanonicalTagForAlias(Tag);
            var platformHash = Platform == PlatformType.WinAny ? 0 : (int)Platform;
            return HashCode.Combine(canonicalTag, platformHash);
        }

        public override bool Equals(object? otherV) 
        {
            if (otherV is null || otherV is not AnnotationID other)
                return false;
            var thisCanonicalTag = AnnotationProcessor.GetCanonicalTagForAlias(Tag);
            var thatCanonicalTag = AnnotationProcessor.GetCanonicalTagForAlias(other.Tag);

            if (thisCanonicalTag != thatCanonicalTag)
                return false;

            if (Platform != PlatformType.WinAny && other.Platform != PlatformType.WinAny && Platform != other.Platform)
                return false;
            return true;
        }

        public static bool operator==(AnnotationID lhs, AnnotationID rhs) {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(AnnotationID lhs, AnnotationID rhs) {
            return !lhs.Equals(rhs);
        }
    }
}
