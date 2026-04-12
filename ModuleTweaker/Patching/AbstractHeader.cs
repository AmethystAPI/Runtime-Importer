using Amethyst.Common.Utility;

namespace Amethyst.ModuleTweaker.Patching {
    // [Header Format V2]
    // [N bytes ] Magic "AME_RTI" (prefixed string)
    // [4 bytes ] Format Version = 2 (uint)
    // [4 bytes ] Flags (uint) - bit 0 = hasDebugNames
    // [4 bytes ] Number of symbols (M)
    // [M symbols]
    // [Classes that inherit add more data here]
    public abstract class AbstractHeader {
        public const string MagicSignature = "AME_RTI";
        public const uint FormatVersion = 2;

        public List<AbstractSymbol> Symbols { get; set; } = [];
        public bool IncludeDebugNames { get; set; } = true;

        public virtual void WriteTo(BinaryWriter writer) {
            writer.WritePrefixedString(MagicSignature);
            writer.Write(FormatVersion);
            uint flags = IncludeDebugNames ? 1u : 0u;
            writer.Write(flags);
            writer.Write(Symbols.Count);
            foreach (var sym in Symbols) {
                sym.IncludeDebugNames = IncludeDebugNames;
                sym.WriteTo(writer);
            }
        }
    }
}
