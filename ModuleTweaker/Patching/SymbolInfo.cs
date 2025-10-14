using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching {
    public class SymbolInfo(uint version, string format, string kind, string name) {
        public SymbolType Type => new(version, format, kind);
        public string Name { get; set; } = name;

        public override bool Equals(object? obj) {
            if (obj is not SymbolInfo other)
                return false;
            return Type == other.Type &&
                   Name == other.Name;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Type, Name);
        }

        public override string ToString() {
            return $"SymbolInfo[{Type}, {Name}]";
        }

        public static bool operator ==(SymbolInfo? a, SymbolInfo? b) => 
            a?.Equals(b) ?? b is null;

        public static bool operator !=(SymbolInfo? a, SymbolInfo? b) =>
            !(a == b);
    }
}
