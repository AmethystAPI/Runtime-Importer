using AsmResolver.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching {
    public enum HeaderFormat {
        PE
    }

    public interface IHeaderContext {
        HeaderFormat GetFormat();
    }

    public class ImporterHeader {
        public const string Magic = "RTIH";
        public HeaderFormat Format { get; set; } = HeaderFormat.PE;
        public uint Version { get; set; } = 1;
        public List<ImportedSymbol> Symbols { get; set; } = [];

        public virtual void Write(IHeaderContext ctx, BinaryWriter writer) {
            // Version 1 layout:
            // [1 byte ] Format (0 = PE)
            // [4 bytes] Magic "RTIH"
            // [4 bytes] Version (1)
            // [8 bytes] Size of symbols section
            // [4 bytes] Number of symbols
            // [N bytes] Symbols
            // [Classes that inherit can add more data here]

            // Validate context format
            if (ctx.GetFormat() != Format)
                throw new InvalidOperationException($"Header format mismatch. Context format is {ctx.GetFormat()}, but header format is {Format}.");

            // Write magic, format and version
            writer.Write(Encoding.ASCII.GetBytes(Magic));
            writer.Write((byte)Format);
            writer.Write(Version);

            // Reserve space for symbol block size
            long sizePos = writer.BaseStream.Position;
            writer.Write(0L);

            // Write symbol count and symbols
            writer.Write(Symbols.Count);

            // Remember position to calculate size later
            long lastPos = writer.BaseStream.Position;

            // Write symbols
            foreach (var symbol in Symbols)
                symbol.Write(writer);

            // Get end position
            long endPos = writer.BaseStream.Position;

            // Calculate symbol block size
            long symbolsSize = endPos - lastPos;

            // Seek back and write symbol block size
            writer.BaseStream.Seek(sizePos, SeekOrigin.Begin);
            writer.Write(symbolsSize);

            // Seek back to end
            writer.BaseStream.Seek(endPos, SeekOrigin.Begin);
        }
    }
}
