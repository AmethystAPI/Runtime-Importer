namespace Amethyst.ModuleTweaker.Patching.PE {
    public abstract class AbstractPESymbol : AbstractSymbol {
        public uint TargetOffset { get; set; } = 0;
        public bool HasStorage { get; set; } = false;
        public uint StorageOffset { get; set; } = 0;

        public override void WriteTo(BinaryWriter writer) {
            base.WriteTo(writer);
            writer.Write(TargetOffset);
            writer.Write((byte)(HasStorage ? 1 : 0));
            writer.Write(StorageOffset);
        }

        public abstract void SetStorage(BinaryWriter writer);
    }
}
