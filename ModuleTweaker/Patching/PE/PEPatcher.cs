using Amethyst.Common.Diagnostics;
using Amethyst.ModuleTweaker.Utility;
using AsmResolver;
using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;
using System.Text;

namespace Amethyst.ModuleTweaker.Patching.PE {
    public class PEPatcher(PEFile file, List<AbstractSymbol> symbols, bool includeDebugNames = true, bool zeroFillImports = false) : IPatcher {
        // Runtime Importer Header
        public const string SectionRTIH = ".rtih"; // Runtime Importer Header
        public const string SectionRTIS = ".rtis"; // Runtime Importer Storage
        public const string SectionNIDT = ".nidt"; // New Import Directory Table

        public static HashSet<string> CustomSections { get; } = [
            SectionRTIH,
            SectionRTIS,
            SectionNIDT
        ];

        public PEFile File { get; } = file;
        public List<AbstractSymbol> Symbols { get; } = symbols;
        public bool IncludeDebugNames { get; } = includeDebugNames;
        public bool ZeroFillImports { get; } = zeroFillImports;

        private record ZeroRange(uint Rva, uint Length, string Purpose);

        // File offsets to zero in the serialized output, computed post-UpdateHeaders and
        // applied by the caller after File.Write. Avoids touching AsmResolver section
        // Contents, which subtly breaks LoadLibrary even when virtual size is preserved.
        public List<(uint FileOffset, uint Length)> PostWriteZeros { get; } = [];

        public bool IsCustomSection(string name) {
            return CustomSections.Contains(name);
        }

        public bool IsPatched() {
            foreach (var section in File.Sections) {
                if (IsCustomSection(section.Name))
                    return true;
            }
            return false;
        }

        public bool Patch() {
            if (IsPatched()) {
                if (!Unpatch())
                    Logger.Fatal("Failed to unpatch existing patch, cannot re-patch.");
            }

            Logger.Debug("Patching PE file...");
            var importDirectory = File.OptionalHeader.GetDataDirectory(DataDirectoryIndex.ImportDirectory);
            if (!importDirectory.IsPresentInPE) {
                Logger.Warn("PE file has no import directory, skipping patch.");
                return false;
            }

            // Read existing import descriptors
            using var importReader = File.CreateDataDirectoryReader(importDirectory).ToReader();
            List<ImportDescriptor> importDescriptors = [];
            List<ImportDescriptor> targetDescriptors = [];
            while (true) {
                var entry = ImportDescriptor.Read(importReader);
                if (entry.IsZero)
                    break;
                string name = File.CreateReaderAtRva(entry.Name).ReadAsciiString();
                if (name.StartsWith("Minecraft.Windows", StringComparison.OrdinalIgnoreCase)) {
                    targetDescriptors.Add(entry);
                }
                importDescriptors.Add(entry);
            }

            if (targetDescriptors.Count == 0) {
                Logger.Warn("PE file does not import from 'Minecraft.Windows', skipping patch.");
                return false;
            }
            if (targetDescriptors.Count > 1) {
                Logger.Warn($"PE file has {targetDescriptors.Count} 'Minecraft.Windows' descriptors; all will be patched.");
            }

            // Map import names to their target RVAs across all matching descriptors
            Dictionary<string, uint> importNameToTarget = [];
            List<AbstractSymbol> symbolsToWrite = [];
            List<ZeroRange> rangesToZero = [];
            HashSet<uint> targetHintNameRvas = [];
            uint totalImportCount = 0;

            foreach (var target in targetDescriptors) {
                uint targetILTRVA = target.OriginalFirstThunk; // hint/name RVAs
                uint targetIATRVA = target.FirstThunk;         // code calls through this; runtime writes here

                var targetILTReader = File.CreateReaderAtRva(targetILTRVA);
                var targetIATReader = File.CreateReaderAtRva(targetIATRVA);
                uint descriptorEntryCount = 0;
                while (true) {
                    ulong iltEntry = targetILTReader.ReadUInt64();
                    if (iltEntry == 0)
                        break;
                    descriptorEntryCount++;
                    totalImportCount++;
                    ulong iatEntry = targetIATReader.ReadUInt64();
                    bool isOrdinal = (iltEntry & 0x8000000000000000) != 0;
                    if (isOrdinal)
                        continue;
                    uint hintNameRVA = (uint)(iltEntry & 0x7FFFFFFFFFFFFFFF);
                    var hintNameReader = File.CreateReaderAtRva(hintNameRVA);
                    ushort hint = hintNameReader.ReadUInt16();
                    string name = hintNameReader.ReadAsciiString();

                    // Zero-fill: record the hint+name blob (2-byte hint + name bytes + null terminator)
                    uint hintNameLen = (uint)(2 + name.Length + 1);
                    rangesToZero.Add(new ZeroRange(hintNameRVA, hintNameLen, $"hint+name:{name}"));
                    targetHintNameRvas.Add(hintNameRVA);

                    var symbol = Symbols.OfType<AbstractPESymbol>().FirstOrDefault(s => s.Name == name);
                    if (symbol is null)
                        continue;
                    uint entryRVA = targetIATRVA + ((descriptorEntryCount - 1) * 8);
                    importNameToTarget[name] = entryRVA;
                    symbol.TargetOffset = entryRVA;
                    symbolsToWrite.Add(symbol);
                    Logger.Info($"Mapping import {name} to target RVA 0x{entryRVA:X}...");
                }

                // Zero-fill: record ILT and IAT slot ranges (entries + 8-byte null terminator)
                uint slotTableBytes = (descriptorEntryCount + 1) * 8;
                rangesToZero.Add(new ZeroRange(targetILTRVA, slotTableBytes, $"ILT ({descriptorEntryCount} entries)"));
                rangesToZero.Add(new ZeroRange(targetIATRVA, slotTableBytes, $"IAT ({descriptorEntryCount} entries)"));

                // Zero-fill: record DLL name string
                string dllName = File.CreateReaderAtRva(target.Name).ReadAsciiString();
                rangesToZero.Add(new ZeroRange(target.Name, (uint)(dllName.Length + 1), $"dll-name:{dllName}"));
            }

            uint index = totalImportCount;

            foreach (var s in Symbols.Where(s => s.IsShadowSymbol && !symbolsToWrite.Contains(s))) {
                symbolsToWrite.Add(s);
                Logger.Debug($"Mapping shadow symbol {s.Name}...");
            }

            uint rtisRealSize = 0;
            // Create the RTIS section
            {
                PESection rtisSec = new(SectionRTIS, SectionFlags.ContentInitializedData | SectionFlags.MemoryRead | SectionFlags.MemoryWrite | SectionFlags.MemoryExecute);
                using var ms = new MemoryStream();
                using var writer = new BinaryWriter(ms, Encoding.UTF8);
                writer.Write(0ul); // Runtime state (8 bytes)
                foreach (var sym in symbolsToWrite) {
                    if (sym is AbstractPESymbol peSym) {
                        if (peSym.HasStorage) {
                            peSym.SetStorage(writer);
                            Logger.Debug($"Assigned storage offset 0x{peSym.StorageOffset:X} to symbol {peSym.Name}...");
                        }
                    }
                }
                byte[] msData = ms.ToArray();
                rtisRealSize = (uint)msData.Length;
                var data = new DataSegment(msData);
                rtisSec.Contents = data;
                File.Sections.Add(rtisSec);
                File.AlignSections();
            }
            Logger.Info($"Generated storage for {symbolsToWrite.Count(s => (s is AbstractPESymbol peSym) && peSym.HasStorage)} symbols, total size 0x{rtisRealSize:X} bytes.");

            // Update storage offsets to be section-relative
            uint rtisRVA = File.Sections.First(s => s.Name == SectionRTIS).Rva;
            foreach (var sym in symbolsToWrite) {
                if (sym is AbstractPESymbol peSym) {
                    if (peSym.HasStorage) {
                        // StorageOffset is RVO now, convert to RVA
                        peSym.StorageOffset += rtisRVA;
                        Logger.Debug($"Fixed up storage RVA to 0x{peSym.StorageOffset:X} for symbol {peSym.Name}...");
                    }
                }
            }

            uint rtihRealSize = 0;
            // Create the RTIH section
            {
                PESection rtihSec = new(SectionRTIH, SectionFlags.ContentInitializedData | SectionFlags.MemoryRead);
                using var ms = new MemoryStream();
                using var writer = new BinaryWriter(ms, Encoding.UTF8);
                var header = new PEImporterHeader() {
                    Symbols = symbolsToWrite,
                    IncludeDebugNames = IncludeDebugNames,
                    OldIDT = importDirectory.VirtualAddress,
                    OldIDTSize = importDirectory.Size,
                    ImportCount = index
                };
                header.WriteTo(writer);
                byte[] msData = ms.ToArray();
                rtihRealSize = (uint)msData.Length;
                var data = new DataSegment(msData);
                rtihSec.Contents = data;
                File.Sections.Add(rtihSec);
                File.AlignSections();
            }
            Logger.Info($"Mapped {symbolsToWrite.Count} symbols, total size 0x{rtihRealSize:X} bytes.");

            // Create new NIDT section
            {
                PESection nidtSec = new(SectionNIDT, SectionFlags.ContentInitializedData | SectionFlags.MemoryRead);
                using var ms = new MemoryStream();
                using var writer = new BinaryWriter(ms, Encoding.UTF8);
                foreach (var entry in importDescriptors) {
                    if (targetDescriptors.Any(t => t.OriginalFirstThunk == entry.OriginalFirstThunk && t.FirstThunk == entry.FirstThunk))
                        continue;
                    entry.Write(writer);
                }
                // Null-terminator descriptor: Windows PE loader iterates until it hits a
                // zero-filled IMAGE_IMPORT_DESCRIPTOR (20 bytes). Without this, the loader
                // reads past the section and LoadLibrary fails.
                writer.Write(new byte[20]);
                var data = new DataSegment(ms.ToArray());
                nidtSec.Contents = data;
                File.Sections.Add(nidtSec);
                File.AlignSections();

                // Update import directory to point to new NIDT
                File.OptionalHeader.SetDataDirectory(DataDirectoryIndex.ImportDirectory, new DataDirectory(nidtSec.Rva, data.GetVirtualSize()));
            }

            if (ZeroFillImports) {
                // Cross-check: filter out any hint/name RVAs that are shared with non-target descriptors
                HashSet<uint> sharedHintNameRvas = [];
                foreach (var entry in importDescriptors) {
                    if (targetDescriptors.Any(t => t.OriginalFirstThunk == entry.OriginalFirstThunk && t.FirstThunk == entry.FirstThunk))
                        continue;
                    var iltReader = File.CreateReaderAtRva(entry.OriginalFirstThunk);
                    while (true) {
                        ulong ilt = iltReader.ReadUInt64();
                        if (ilt == 0) break;
                        if ((ilt & 0x8000000000000000) != 0) continue;
                        uint hnRva = (uint)(ilt & 0x7FFFFFFFFFFFFFFF);
                        if (targetHintNameRvas.Contains(hnRva))
                            sharedHintNameRvas.Add(hnRva);
                    }
                }

                var filteredRanges = rangesToZero
                    .Where(r => !(r.Purpose.StartsWith("hint+name:") && sharedHintNameRvas.Contains(r.Rva)))
                    .ToList();

                // Defer zero-fill to post-write: compute file offsets now, apply to output bytes later.
                // This sidesteps AsmResolver's Contents replacement, which corrupts LoadLibrary.
                File.UpdateHeaders();
                foreach (var range in filteredRanges) {
                    var sec = File.Sections.FirstOrDefault(s =>
                        range.Rva >= s.Rva && (range.Rva + range.Length) <= (s.Rva + s.GetVirtualSize()));
                    if (sec is null) {
                        Logger.Warn($"Range 0x{range.Rva:X}+{range.Length} ({range.Purpose}) has no containing section, skipping.");
                        continue;
                    }
                    uint fileOffset = (uint)sec.Offset + (range.Rva - sec.Rva);
                    PostWriteZeros.Add((fileOffset, range.Length));
                    Logger.Debug($"Queued post-write zero: file 0x{fileOffset:X}+0x{range.Length:X} ({range.Purpose})");
                }
                Logger.Info($"Queued {PostWriteZeros.Count} post-write zero range(s).");

                var boundDir = File.OptionalHeader.GetDataDirectory(DataDirectoryIndex.BoundImportDirectory);
                if (boundDir.IsPresentInPE)
                    File.OptionalHeader.SetDataDirectory(DataDirectoryIndex.BoundImportDirectory, new DataDirectory(0, 0));
                var delayDir = File.OptionalHeader.GetDataDirectory(DataDirectoryIndex.DelayImportDescrDirectory);
                if (delayDir.IsPresentInPE)
                    File.OptionalHeader.SetDataDirectory(DataDirectoryIndex.DelayImportDescrDirectory, new DataDirectory(0, 0));

                // Clear the Debug data directory entry so external tools don't find CodeView/PDB info.
                // We deliberately do NOT touch the raw bytes the directory points at: they live in .rdata
                // adjacent to LoadConfig/CFG structures that the Windows loader actively reads, and
                // zeroing them crashes LoadLibrary. The build-time audit in mod_build.lua fails if any
                // RSDS or .pdb string actually appears in the output, which is the real defense.
                // var debugDir = File.OptionalHeader.GetDataDirectory(DataDirectoryIndex.DebugDirectory);
                // if (debugDir.IsPresentInPE)
                //     File.OptionalHeader.SetDataDirectory(DataDirectoryIndex.DebugDirectory, new DataDirectory(0, 0));
            }

            File.UpdateHeaders();
            Logger.Info("PE file patched successfully.");
            return true;
        }

        public bool RemoveSection(string name) {
            var section = File.Sections.FirstOrDefault(s => s.Name == name);
            if (section is not null) {
                return File.Sections.Remove(section);
            }
            return false;
        }

        public bool Unpatch() {
            for (int i = File.Sections.Count - 1; i >= 0; i--) {
                var section = File.Sections[i]!;
                if (section.Name == SectionRTIH) {
                    // Read just the header fields we need to restore IDT
                    var asmReader = section.CreateReader();
                    byte[] sectionData = asmReader.ReadToEnd();
                    using var br = new BinaryReader(new MemoryStream(sectionData));

                    // Read magic (prefixed string)
                    int magicLen = br.ReadInt32();
                    br.ReadBytes(magicLen);
                    // Read version
                    uint ver = br.ReadUInt32();
                    if (ver == 2) {
                        // V2 format: flags, symbol count, skip symbols, then OldIDT/OldIDTSize
                        uint flags = br.ReadUInt32();
                        bool hasDebugNames = (flags & 1) != 0;
                        int symbolCount = br.ReadInt32();
                        // Skip all symbols - we just need OldIDT/OldIDTSize at the end
                        // Unfortunately we need to parse through them to find the end
                        for (int s = 0; s < symbolCount; s++) {
                            byte kind = br.ReadByte();        // KindTag
                            br.ReadUInt64();                  // NameHash
                            if (hasDebugNames) {
                                int nameLen = br.ReadInt32();
                                br.ReadBytes(nameLen);        // DebugName
                            }
                            br.ReadUInt32();                  // TargetOffset
                            bool hasStorage = br.ReadByte() != 0;
                            br.ReadUInt32();                  // StorageOffset

                            if (kind == 0) { // Function
                                bool isDestructor = br.ReadByte() != 0;
                                bool isVirtual = br.ReadByte() != 0;
                                if (isVirtual) {
                                    br.ReadUInt32();          // VirtualIndex
                                    br.ReadUInt64();          // VirtualTableHash
                                    if (hasDebugNames) {
                                        int vtLen = br.ReadInt32();
                                        br.ReadBytes(vtLen);  // DebugVtName
                                    }
                                } else {
                                    bool isSig = br.ReadByte() != 0;
                                    if (isSig) {
                                        uint count = br.ReadUInt32();
                                        br.ReadBytes((int)(count * 2)); // compiled sig elements
                                    } else {
                                        br.ReadUInt64();      // Address
                                    }
                                }
                            } else if (kind == 1) { // Data
                                br.ReadByte();                // IsVirtualTableAddress
                                br.ReadByte();                // IsVirtualTable
                                bool isSig = br.ReadByte() != 0;
                                if (isSig) {
                                    uint count = br.ReadUInt32();
                                    br.ReadBytes((int)(count * 2));
                                } else {
                                    br.ReadUInt64();          // Address
                                }
                            }
                        }
                        uint oldIDT = br.ReadUInt32();
                        uint oldIDTSize = br.ReadUInt32();
                        File.OptionalHeader.SetDataDirectory(DataDirectoryIndex.ImportDirectory, new DataDirectory(oldIDT, oldIDTSize));
                    } else {
                        Logger.Warn($"RTIH section has unknown format version {ver}, cannot restore IDT.");
                    }
                }

                if (IsCustomSection(section.Name)) {
                    Logger.Debug($"Removing custom section '{section.Name}'...");
                    File.Sections.RemoveAt(i);
                }
            }
            File.AlignSections();
            return true;
        }
    }
}
