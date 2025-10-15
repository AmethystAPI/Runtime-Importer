using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching {
    public class HeaderType(uint version, string type) {
        public uint Version { get; set; } = version;
        public string Type { get; set; } = type;

        public override bool Equals(object? obj) {
            if (obj is not HeaderType other)
                return false;
            return Version == other.Version && Type == other.Type;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Version, Type);
        }

        public override string ToString() {
            return $"HeaderType[v{Version}, {Type}]";
        }

        public static bool operator ==(HeaderType? a, HeaderType? b) =>
            a?.Equals(b) ?? b is null;

        public static bool operator !=(HeaderType? a, HeaderType? b) =>
            !(a == b);
    }
}
