using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching {
    public static class SymbolFactory {
        private static readonly Dictionary<SymbolType, Func<AbstractSymbol>> sConstructors = [];
        public static IReadOnlyDictionary<SymbolType, Func<AbstractSymbol>> Constructors => sConstructors;

        public static void Register(SymbolType type, Func<AbstractSymbol> constructor) {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(constructor);
            if (sConstructors.ContainsKey(type))
                throw new InvalidOperationException($"A constructor for {type} is already registered.");
            sConstructors[type] = constructor;
        }

        public static AbstractSymbol Create(SymbolType type) {
            if (sConstructors.TryGetValue(type, out var constructor))
                return constructor();
            throw new InvalidOperationException($"No constructor registered for {type}.");
        }
    }
}
