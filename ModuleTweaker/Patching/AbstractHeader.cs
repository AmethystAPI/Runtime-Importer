using Amethyst.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching {
    // [Header Format]
    // [8 bytes ] Magic "AME_RTI"
    // [4 bytes ] Format Version (uint)
    // [4 bytes ] Format Type length (N)
    // [N bytes ] Format Type (UTF-8)
    // [4 bytes ] Number of symbols (M)
    // [M symbols]
    // [Classes that inherit add more data here]
    public abstract class AbstractHeader {
        public const string MagicSignature = "AME_RTI";

        public List<AbstractSymbol> Symbols { get; set; } = [];

        public abstract string FormatType { get; }
        public abstract uint FormatVersion { get; }

        public virtual void WriteTo(BinaryWriter writer) {
            writer.WritePrefixedString(MagicSignature);
            writer.Write(FormatVersion);
            writer.WritePrefixedString(FormatType);
            writer.Write(Symbols.Count);
            foreach (var sym in Symbols)
                sym.WriteTo(writer);
        }

        public virtual void ReadFrom(BinaryReader reader) {
            string magic = reader.ReadPrefixedString();
            if (magic != MagicSignature)
                throw new InvalidOperationException($"Invalid magic signature '{magic}', expected '{MagicSignature}'.");
            uint ver = reader.ReadUInt32();
            if (ver != FormatVersion)
                throw new InvalidOperationException($"Incompatible header version {ver}, expected {FormatVersion}.");
            string fmt = reader.ReadPrefixedString();
            if (fmt != FormatType)
                throw new InvalidOperationException($"Incompatible header format {fmt}, expected {FormatType}.");
            int count = reader.ReadInt32();
            Symbols.Clear();
            for (int i = 0; i < count; i++) {
                SymbolInfo info = AbstractSymbol.PeekInfo(reader);
                AbstractSymbol sym = SymbolFactory.Create(info.Type);
                sym.ReadFrom(reader);
                Symbols.Add(sym);
            }
        }

        public static HeaderType PeekInfo(BinaryReader reader) {
            long initialPos = reader.BaseStream.Position;
            try {
                string magic = reader.ReadPrefixedString();
                if (magic != MagicSignature)
                    throw new InvalidOperationException($"Invalid magic signature '{magic}', expected '{MagicSignature}'.");
                uint ver = reader.ReadUInt32();
                string fmt = reader.ReadPrefixedString();
                return new(ver, fmt);
            }
            finally {
                reader.BaseStream.Position = initialPos;
            }
        }
    }
}
