using Amethyst.Common.Utility;

namespace Amethyst.ModuleTweaker.Patching.PE {
    public class PEDataSymbol : AbstractPESymbol {
        public override byte KindTag => 1;
        public override bool IsShadowSymbol => IsVirtualTable;

        public bool IsVirtualTableAddress { get; set; } = false;
        public bool IsVirtualTable { get; set; } = false;
        public ulong Address { get; set; } = 0x0;
        public bool IsSignature { get; set; } = false;
        public string Signature { get; set; } = string.Empty;

        public override void WriteTo(BinaryWriter writer) {
            base.WriteTo(writer);
            writer.Write((byte)(IsVirtualTableAddress ? 1 : 0));
            writer.Write((byte)(IsVirtualTable ? 1 : 0));
            writer.Write((byte)(IsSignature ? 1 : 0));
            if (IsSignature)
                writer.WriteCompiledSignature(Signature);
            else
                writer.Write(Address);
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
