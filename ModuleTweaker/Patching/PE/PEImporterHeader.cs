using AsmResolver.PE.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching.PE {
    public class PEHeaderContext(PEFile file, uint iatVirtualAddress, uint iatCount) : IHeaderContext {
        public HeaderFormat GetFormat() => HeaderFormat.PE;

        public PEFile File { get; } = file;
        public uint IATVirtualAddress { get; } = iatVirtualAddress;
        public uint IATCount { get; } = iatCount;
    }

    public class PEImporterHeader : ImporterHeader {
        public override void Write(IHeaderContext ctx, BinaryWriter writer) {
            // Version 1 PE-specific layout:
            // [Base ImporterHeader layout]
            // [4 bytes] "Minecraft.Windows.exe" IAT Virtual Address
            // [4 bytes] "Minecraft.Windows.exe" IAT Count

            if (ctx is not PEHeaderContext peCtx)
                throw new InvalidOperationException($"Invalid header context type. Expected PEHeaderContext, got {ctx.GetType()}.");

            // Call base to write common layout
            // Write PE-specific fields
            base.Write(ctx, writer);
            writer.Write(peCtx.IATVirtualAddress);
            writer.Write(peCtx.IATCount);
        }
    }
}
