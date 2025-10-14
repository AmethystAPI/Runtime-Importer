using Amethyst.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching {
    public abstract class AbstractSymbol {
        public string Name { get; set; } = string.Empty;

        public abstract string FormatType { get; }
        public abstract uint FormatVersion { get; }
        public abstract string Kind { get; }
        public abstract bool IsShadowSymbol { get; }

        public virtual void WriteTo(BinaryWriter writer) {
            writer.Write(FormatVersion);
            writer.WritePrefixedString(FormatType);
            writer.WritePrefixedString(Kind);
            writer.WritePrefixedString(Name);
        }

        public virtual void ReadFrom(BinaryReader reader) {
            uint ver = reader.ReadUInt32();
            if (ver != FormatVersion)
                throw new InvalidOperationException($"Incompatible symbol version {ver}, expected {FormatVersion}.");
            string fmt = reader.ReadPrefixedString();
            if (fmt != FormatType)
                throw new InvalidOperationException($"Incompatible symbol format {fmt}, expected {FormatType}.");
            string kind = reader.ReadPrefixedString();
            if (kind != Kind)
                throw new InvalidOperationException($"Incompatible symbol kind {kind}, expected {Kind}.");
            Name = reader.ReadPrefixedString();
        }

        public override string ToString() {
            return $"Symbol[v{FormatVersion}, {FormatType}, {Name}, {Kind}]";
        }

        public static SymbolInfo PeekInfo(BinaryReader reader) {
            long initialPos = reader.BaseStream.Position;
            try {
                uint ver = reader.ReadUInt32();
                string fmt = reader.ReadPrefixedString();
                string kind = reader.ReadPrefixedString();
                return new(ver, fmt, kind);
            } finally {
                reader.BaseStream.Position = initialPos;
            }
        }
    }
}
