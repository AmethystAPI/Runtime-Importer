using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching.PE {
    public abstract class AbstractPESymbol : AbstractSymbol {
        public override string FormatType => "pe32+";
        public uint TargetOffset { get; set; } = 0;
        public bool HasStorage { get; set; } = false;
        public uint StorageOffset { get; set; } = 0;

        public override void ReadFrom(BinaryReader reader) {
            base.ReadFrom(reader);
            TargetOffset = reader.ReadUInt32();
            HasStorage = reader.ReadByte() != 0;
            StorageOffset = reader.ReadUInt32();
        }

        public override void WriteTo(BinaryWriter writer) {
            base.WriteTo(writer);
            writer.Write(TargetOffset);
            writer.Write((byte)(HasStorage ? 1 : 0));
            writer.Write(StorageOffset);
        }

        public abstract void SetStorage(BinaryWriter writer);
    }
}
