using Amethyst.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching.PE.V1 {
    public class PEDataSymbol : AbstractPESymbol {
        public override uint FormatVersion => 1;
        public override string Kind => "data";

        public bool IsVirtualTableAddress { get; set; } = false;
        public bool IsVirtualTable { get; set; } = false;
        public ulong Address { get; set; } = 0x0;

        public override bool IsShadowSymbol => IsVirtualTable;

        public override void WriteTo(BinaryWriter writer) {
            base.WriteTo(writer);
            writer.Write((byte)(IsVirtualTableAddress ? 1 : 0));
            writer.Write((byte)(IsVirtualTable ? 1 : 0));
            writer.Write(Address);
        }

        public override void ReadFrom(BinaryReader reader) {
            base.ReadFrom(reader);
            IsVirtualTableAddress = reader.ReadByte() != 0;
            IsVirtualTable = reader.ReadByte() != 0;
            Address = reader.ReadUInt64();
        }

        public override void SetStorage(BinaryWriter writer) {
            if (!HasStorage)
                throw new InvalidOperationException("Cannot set storage on a symbol without storage.");
            writer.Align(8, 0x00); // Align to 8 bytes with zeros
            StorageOffset = (uint)writer.BaseStream.Position;
            writer.Write(new byte[8]);
        }
    }
}
