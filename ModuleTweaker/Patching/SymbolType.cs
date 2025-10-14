using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching {
    public class SymbolType(uint version, string kind) {
        public uint Version { get; set; } = version;
        public string Kind { get; set; } = kind;

        public override bool Equals(object? obj) {
            if (obj is not SymbolType other)
                return false;
            return Version == other.Version && Kind == other.Kind;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Version, Kind);
        }

        public override string ToString() {
            return $"SymbolType[v{Version}, {Kind}]";
        }

        public static bool operator ==(SymbolType? a, SymbolType? b) =>
            a?.Equals(b) ?? b is null;

        public static bool operator !=(SymbolType? a, SymbolType? b) =>
            !(a == b);
    }
}
