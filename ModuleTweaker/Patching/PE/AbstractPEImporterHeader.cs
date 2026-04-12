namespace Amethyst.ModuleTweaker.Patching.PE {
    // [Header Format]
    // [Includes AbstractHeader layout]
    // [4 bytes ] Old IDT RVA
    // [4 bytes ] Old IDT Size
    // [4 bytes ] Import Count

    public abstract class AbstractPEImporterHeader : AbstractHeader {
        public uint OldIDT { get; set; } = 0;
        public uint OldIDTSize { get; set; } = 0;
        public uint ImportCount { get; set; } = 0;

        public override void WriteTo(BinaryWriter writer) {
            base.WriteTo(writer);
            writer.Write(OldIDT);
            writer.Write(OldIDTSize);
            writer.Write(ImportCount);
        }
    }
}
