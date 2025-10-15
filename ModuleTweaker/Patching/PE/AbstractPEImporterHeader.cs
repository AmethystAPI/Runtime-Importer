namespace Amethyst.ModuleTweaker.Patching.PE {
    // [Header Format]
    // [Includes AbstractHeader layout]
    // [4 bytes ] Old IDT RVA
    // [4 bytes ] Old IDT Size
    // [4 bytes ] Import Count

    public abstract class AbstractPEImporterHeader() : AbstractHeader {
        public override string FormatType => "pe32+";

        public uint OldIDT { get; set; } = 0;
        public uint OldIDTSize { get; set; } = 0;
        public uint ImportCount { get; set; } = 0;

        public override void ReadFrom(BinaryReader reader) {
            base.ReadFrom(reader);
            OldIDT = reader.ReadUInt32();
            OldIDTSize = reader.ReadUInt32();
            ImportCount = reader.ReadUInt32();
        }

        public override void WriteTo(BinaryWriter writer) {
            base.WriteTo(writer);
            writer.Write(OldIDT);
            writer.Write(OldIDTSize);
            writer.Write(ImportCount);
        }
    }
}
