using System.Runtime.InteropServices;

namespace Amethyst.ModuleTweaker.Patching.PE {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ImportDescriptor()
    {
        public static uint Size => (uint)Marshal.SizeOf<ImportDescriptor>();
        public static ImportDescriptor Empty => new();

        public uint OriginalFirstThunk = 0;
        public int TimeDateStamp = 0;
        public uint ForwarderChain = 0;
        public uint Name = 0;
        public uint FirstThunk = 0;

        public bool IsZero
        {
            get
            {
                Span<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
                foreach (var b in bytes)
                    if (b != 0) return false;
                return true;
            }
        }

        public static ImportDescriptor Read(BinaryReader reader)
        {
            Span<byte> bytes = stackalloc byte[(int)Size];
            if (reader.Read(bytes) != Size)
                throw new ArgumentException("Not enough data to read ImportDescriptor.");
            return MemoryMarshal.Read<ImportDescriptor>(bytes);
        }

        public void Write(BinaryWriter writer)
        {
            Span<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
            writer.Write(bytes);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not ImportDescriptor other)
                return false;

            Span<byte> selfBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
            Span<byte> otherBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref other, 1));
            return selfBytes.SequenceEqual(otherBytes);
        }

        public override int GetHashCode()
        {
            Span<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
            int hash = 17;
            foreach (var b in bytes)
                hash = hash * 31 + b;
            return hash;
        }

        public static implicit operator bool(ImportDescriptor descriptor) => !descriptor.IsZero;
        public static bool operator ==(ImportDescriptor left, ImportDescriptor right) => left.Equals(right);
        public static bool operator !=(ImportDescriptor left, ImportDescriptor right) => !left.Equals(right);
    }
}
