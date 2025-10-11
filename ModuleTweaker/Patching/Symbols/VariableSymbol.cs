using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching.Symbols {
    public class VariableSymbol : ImportedSymbol {
        public ulong Address { get; set; } = 0;

        public override SymbolType GetSymbolType() {
            return SymbolType.Variable;
        }

        public override void Write(BinaryWriter writer) {
            // Version 1 VariableSymbol-specific format:
            // [Base ImporterSymbol layout]
            // [8 bytes] Address

            // Write base class data first
            base.Write(writer);
            writer.Write(Address);
        }
    }
}
