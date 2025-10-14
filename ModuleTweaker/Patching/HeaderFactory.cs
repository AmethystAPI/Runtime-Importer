using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching {
    public class HeaderFactory {
        private static readonly Dictionary<HeaderType, Func<object[], AbstractHeader>> sConstructors = [];
        public static IReadOnlyDictionary<HeaderType, Func<object[], AbstractHeader>> Constructors => sConstructors;

        public static void Register(HeaderType type, Func<object[], AbstractHeader> constructor) {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(constructor);
            if (sConstructors.ContainsKey(type))
                throw new InvalidOperationException($"A constructor for {type} is already registered.");
            sConstructors[type] = constructor;
        }

        public static AbstractHeader Create(HeaderType type, params object[] args) {
            if (sConstructors.TryGetValue(type, out var constructor))
                return constructor(args);
            throw new InvalidOperationException($"No constructor registered for {type}.");
        }
    }
}
