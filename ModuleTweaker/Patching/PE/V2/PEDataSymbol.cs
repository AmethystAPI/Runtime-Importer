using Amethyst.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching.PE.V2 {
    public class PEDataSymbol : V1.PEDataSymbol {
        public override uint FormatVersion => 2;

        public bool IsSignature { get; set; } = false;
        public string Signature { get; set; } = string.Empty;

        public override void WriteTo(BinaryWriter writer) {
            base.WriteTo(writer);
            writer.Write((byte)(IsSignature ? 1 : 0));
            if (IsSignature)
                writer.WritePrefixedString(Signature);
        }

        public override void ReadFrom(BinaryReader reader) {
            base.ReadFrom(reader);
            IsSignature = reader.ReadByte() != 0;
            if (IsSignature)
                Signature = reader.ReadPrefixedString();
        }
    }
}
