using Amethyst.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching.PE.V1 {
    public class PEFunctionSymbol : AbstractPESymbol {
        public override uint FormatVersion => 1;
        public override string Kind => "function";

        public override bool IsShadowSymbol => false;

        public bool IsDestructor { get; set; } = false;
        public bool IsVirtual { get; set; } = false;
        public uint VirtualIndex { get; set; } = 0;
        public string VirtualTable { get; set; } = string.Empty;
        public bool IsSignature { get; set; } = false;
        public string Signature { get; set; } = string.Empty;
        public ulong Address { get; set; } = 0x0;

        public override void ReadFrom(BinaryReader reader) {
            base.ReadFrom(reader);
            IsDestructor = reader.ReadByte() != 0;
            IsVirtual = reader.ReadByte() != 0;
            if (IsVirtual) {
                VirtualIndex = reader.ReadUInt32();
                VirtualTable = reader.ReadPrefixedString();
            }
            else {
                IsSignature = reader.ReadByte() != 0;
                if (IsSignature)
                    Signature = reader.ReadPrefixedString();
                else
                    Address = reader.ReadUInt64();
            }
        }

        public override void WriteTo(BinaryWriter writer) {
            base.WriteTo(writer);
            writer.Write((byte)(IsDestructor ? 1 : 0));
            writer.Write((byte)(IsVirtual ? 1 : 0));
            if (IsVirtual) {
                writer.Write(VirtualIndex);
                writer.WritePrefixedString(VirtualTable);
            }
            else {
                writer.Write((byte)(IsSignature ? 1 : 0));
                if (IsSignature)
                    writer.WritePrefixedString(Signature);
                else
                    writer.Write(Address);
            }
        }
    }
}
