using Amethyst.Common.Utility;

namespace Amethyst.ModuleTweaker.Patching {
    public abstract class AbstractSymbol {
        public string Name { get; set; } = string.Empty;
        public bool IncludeDebugNames { get; set; } = true;

        public abstract byte KindTag { get; }
        public abstract bool IsShadowSymbol { get; }

        public virtual void WriteTo(BinaryWriter writer) {
            writer.Write(KindTag);
            writer.WriteHashedName(Name, IncludeDebugNames);
        }

        public override string ToString() {
            return $"Symbol[kind={KindTag}, {Name}]";
        }
    }
}
