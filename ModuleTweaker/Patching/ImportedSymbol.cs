using AsmResolver.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching {
    public enum SymbolType {
        Function,
        Variable,
        VirtualPointer
    }

    public abstract class ImportedSymbol {
        public string Name { get; set; } = string.Empty;

        public abstract SymbolType GetSymbolType();

        public virtual void Write(BinaryWriter writer) {
            // Version 1 layout:
            // [1 byte ] SymbolType (0 = Function, 1 = Variable, 2 = VirtualPointer)
            // [4 bytes] Name length (N)
            // [N bytes] Name (UTF-8)

            writer.Write((byte)GetSymbolType());
            writer.Write(Encoding.UTF8.GetByteCount(Name));
            writer.Write(Encoding.UTF8.GetBytes(Name));
        }
    }
}
