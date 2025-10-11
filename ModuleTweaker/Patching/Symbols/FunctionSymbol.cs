using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching.Symbols {
    public class FunctionSymbol : ImportedSymbol {
        public bool IsVirtual { get; set; } = false;

        // If IsVirtual is true, these fields are used.
        public uint VirtualIndex { get; set; } = 0;
        public string VirtualTable { get; set; } = string.Empty;

        // If IsVirtual is false, these fields are used.
        public bool IsSignature { get; set; } = false;

        // If IsSignature is true, this field is used.
        public string Signature { get; set; } = string.Empty;

        // If IsSignature is false, this field is used.
        public ulong Address { get; set; } = 0;

        public override SymbolType GetSymbolType() {
            return SymbolType.Function;
        }

        public override void Write(BinaryWriter writer) {
            // Version 1 FunctionSymbol-specific format:
            // [Base ImporterSymbol layout]
            // [1 byte ] IsVirtual (0 = false, 1 = true)
            // If IsVirtual == 1:
            //   [4 bytes] VirtualIndex
            //   [4 bytes] VirtualTable length (N)
            //   [N bytes] VirtualTable (UTF-8)
            // Else (IsVirtual == 0):
            //   [1 byte ] IsSignature (0 = false, 1 = true)
            //   If IsSignature == 1:
            //     [4 bytes] Signature length (N)
            //     [N bytes] Signature (UTF-8)
            //   Else (IsSignature == 0):
            //     [8 bytes] Address

            // Write base class data first
            base.Write(writer);

            writer.Write((byte)(IsVirtual ? 1 : 0));
            if (IsVirtual) {
                writer.Write(VirtualIndex);
                writer.Write(Encoding.UTF8.GetByteCount(VirtualTable));
                writer.Write(Encoding.UTF8.GetBytes(VirtualTable));
            }
            else {
                writer.Write((byte)(IsSignature ? 1 : 0));
                if (IsSignature) {
                    writer.Write(Encoding.UTF8.GetByteCount(Signature));
                    writer.Write(Encoding.UTF8.GetBytes(Signature));
                }
                else {
                    writer.Write(Address);
                }
            }
        }
    }
}
