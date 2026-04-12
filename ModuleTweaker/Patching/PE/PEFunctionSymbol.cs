using Amethyst.Common.Utility;

namespace Amethyst.ModuleTweaker.Patching.PE {
    public class PEFunctionSymbol : AbstractPESymbol {
        public static readonly byte[] VirtualDestructorDeletingDisableBlock = [
            0x48, 0xb8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, // mov rax, 0x1000000000000000
            0x31, 0xD2,                                                   // xor edx, edx (sets delete flag to false)
            0xFF, 0xE0                                                    // jmp rax
        ];

        public override byte KindTag => 0;
        public override bool IsShadowSymbol => false;

        public bool IsDestructor { get; set; } = false;
        public bool IsVirtual { get; set; } = false;
        public uint VirtualIndex { get; set; } = 0;
        public string VirtualTable { get; set; } = string.Empty;
        public bool IsSignature { get; set; } = false;
        public string Signature { get; set; } = string.Empty;
        public ulong Address { get; set; } = 0x0;

        public override void WriteTo(BinaryWriter writer) {
            base.WriteTo(writer);
            writer.Write((byte)(IsDestructor ? 1 : 0));
            writer.Write((byte)(IsVirtual ? 1 : 0));
            if (IsVirtual) {
                writer.Write(VirtualIndex);
                writer.WriteHashedName(VirtualTable, IncludeDebugNames);
            }
            else {
                writer.Write((byte)(IsSignature ? 1 : 0));
                if (IsSignature)
                    writer.WriteCompiledSignature(Signature);
                else
                    writer.Write(Address);
            }
        }

        public override void SetStorage(BinaryWriter writer) {
            if (!HasStorage)
                throw new InvalidOperationException("Cannot set storage on a symbol without storage.");
            if (!IsDestructor)
                throw new InvalidOperationException("Cannot set storage on a non-destructor function symbol.");
            writer.Align(16, 0x90); // Align to 16 bytes with NOPs
            StorageOffset = (uint)writer.BaseStream.Position;
            writer.Write(VirtualDestructorDeletingDisableBlock);
        }
    }
}
