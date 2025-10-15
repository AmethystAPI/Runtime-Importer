using Amethyst.Common.Diagnostics;
using Amethyst.ModuleTweaker.Patching.PE.V1;
using Amethyst.ModuleTweaker.Utility;
using AsmResolver;
using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;
using System.Text;

namespace Amethyst.ModuleTweaker.Patching.PE {
    public class PEPatcher(PEFile file, List<AbstractSymbol> symbols) : IPatcher {
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
            // Version 1 full PE-specific layout:
            // .rtih - Runtime Importer Header
            // [PEImporterHeader data]

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
            uint targetIATRVA = 0;
            uint targetILTRVA = 0;
            while (true) {
                var entry = ImportDescriptor.Read(importReader);
                if (entry.IsZero)
                    break;
                string name = File.CreateReaderAtRva(entry.Name).ReadAsciiString();
                if (name.StartsWith("Minecraft.Windows", StringComparison.OrdinalIgnoreCase)) {
                    targetIATRVA = entry.OriginalFirstThunk;
                    targetILTRVA = entry.FirstThunk;
                }
                importDescriptors.Add(entry);
            }

            if (targetIATRVA == 0 || targetILTRVA == 0) {
                Logger.Warn("PE file does not import from 'Minecraft.Windows', skipping patch.");
                return false;
            }

            // Map import names to their target RVAs
            Dictionary<string, uint> importNameToTarget = [];
            List<AbstractSymbol> symbolsToWrite = [];
            var targetILTReader = File.CreateReaderAtRva(targetILTRVA);
            var targetIATReader = File.CreateReaderAtRva(targetIATRVA);
            uint index = 0;
            while (true) {
                ulong iltEntry = targetILTReader.ReadUInt64();
                if (iltEntry == 0)
                    break;
                index++;
                ulong iatEntry = targetIATReader.ReadUInt64();
                bool isOrdinal = (iltEntry & 0x8000000000000000) != 0;
                if (isOrdinal) {
                    continue;
                }
                uint hintNameRVA = (uint)(iltEntry & 0x7FFFFFFFFFFFFFFF);
                var hintNameReader = File.CreateReaderAtRva(hintNameRVA);
                ushort hint = hintNameReader.ReadUInt16();
                string name = hintNameReader.ReadAsciiString();
                var symbol = Symbols.OfType<AbstractPESymbol>().FirstOrDefault(s => s.Name == name);
                if (symbol is null)
                    continue;
                uint entryRVA = targetILTRVA + ((index - 1) * 8);
                importNameToTarget[name] = entryRVA;
                symbol.TargetOffset = entryRVA;
                symbolsToWrite.Add(symbol);
                Logger.Debug($"Mapping import {name} to target RVA 0x{entryRVA:X}...");
            }

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
                    if (entry.OriginalFirstThunk == targetIATRVA || entry.FirstThunk == targetILTRVA)
                        continue;
                    entry.Write(writer);
                }
                var data = new DataSegment(ms.ToArray());
                nidtSec.Contents = data;
                File.Sections.Add(nidtSec);
                File.AlignSections();

                // Update import directory to point to new NIDT
                File.OptionalHeader.SetDataDirectory(DataDirectoryIndex.ImportDirectory, new DataDirectory(nidtSec.Rva, data.GetVirtualSize()));
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
                    using var reader = section.CreateReader().ToReader();
                    var type = AbstractHeader.PeekInfo(reader);
                    var header = HeaderFactory.Create(type);
                    header.ReadFrom(reader);

                    if (header is not AbstractPEImporterHeader peHeader) {
                        Logger.Warn($"RTIH section does not contain a valid PE Importer Header, cannot unpatch.");
                        return false;
                    }

                    // Restore the old import directory
                    File.OptionalHeader.SetDataDirectory(DataDirectoryIndex.ImportDirectory, new DataDirectory(peHeader.OldIDT, peHeader.OldIDTSize));
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
