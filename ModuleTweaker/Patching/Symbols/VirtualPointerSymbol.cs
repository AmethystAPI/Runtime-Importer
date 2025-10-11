using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching.Symbols {
    public class VirtualPointerSymbol : ImportedSymbol {
        public ulong Address { get; set; } = 0;

        public override SymbolType GetSymbolType() {
            return SymbolType.VirtualPointer;
        }

        public override void Write(BinaryWriter writer) {
            // Version 1 VirtualPointerSymbol-specific format:
            // [Base ImporterSymbol layout]
            // [8 bytes] Address

            // Write base class data first
            base.Write(writer);

            writer.Write(Address);
        }
    }
}
