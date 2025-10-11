using Amethyst.Common.Diagnostics;
using AsmResolver;
using AsmResolver.IO;
using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching.PE {
    public class PEPatcher(PEFile file, List<ImportedSymbol> symbols) : IPatcher {
        // Runtime Importer Header
        public const string SectionRTIH = ".rtih";

        // New Import Descriptor Table
        public const string SectionIDNEW = ".idnew";

        public static HashSet<string> CustomSections { get; } = [
            SectionRTIH,
            SectionIDNEW
        ];

        public PEFile File { get; } = file;
        public List<ImportedSymbol> Symbols { get; } = symbols;

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
            // .idnew - New Import Descriptor Table
            // [4 bytes] Original IDT RVA
            // [4 bytes] Original IDT Size
            // [New IDT data (old IDT minus "Minecraft.Windows.exe")]

            if (IsPatched()) {
                Logger.Debug("PE file is already patched, unpatching it first.");
                Unpatch();
            }

            Logger.Debug("Patching PE file for runtime importing...");

            // Get the import directory entries
            var importDirectory = File.OptionalHeader.GetDataDirectory(DataDirectoryIndex.ImportDirectory);
            if (!importDirectory.IsPresentInPE) {
                Logger.Warn("PE file has no import directory, no reason to patch it.");
                return false;
            }

            // Read existing import descriptors
            List<ImportDescriptor> importDescriptors = [];
            {
                var importDirectoryReader = File.CreateDataDirectoryReader(importDirectory);
                while (importDirectoryReader.CanRead(ImportDescriptor.Size)) {
                    var descriptor = ImportDescriptor.Read(ref importDirectoryReader);
                    if (descriptor.IsZero)
                        break;
                    importDescriptors.Add(descriptor);
                }
            }

            // Find the import descriptor for "Minecraft.Windows.exe"
            ImportDescriptor? minecraftWindowsImportDescriptor = null;
            foreach (var descriptor in importDescriptors) {
                var nameRva = descriptor.Name;
                var name = File.CreateReaderAtRva(nameRva).ReadAsciiString();
                if (name.StartsWith("Minecraft.Windows", StringComparison.OrdinalIgnoreCase)) {
                    minecraftWindowsImportDescriptor = descriptor;
                    break;
                }
            }

            // If not found, no reason to patch
            if (minecraftWindowsImportDescriptor is null) {
                Logger.Warn("PE file does not import from 'Minecraft.Windows.exe', no reason to patch it.");
                return false;
            }

            // Map all mangled names for "Minecraft.Windows.exe" functions to IAT offsets
            uint iatRva = minecraftWindowsImportDescriptor.Value.FirstThunk;
            uint iatCount = 0;
            Dictionary<string, uint> iatIndices = [];
            {
                var reader = File.CreateReaderAtRva(minecraftWindowsImportDescriptor.Value.OriginalFirstThunk);
                uint iatIndex = 0;
                while (reader.CanRead(sizeof(ulong))) {
                    ulong iltEntry = reader.ReadUInt64();
                    if (iltEntry == 0)
                        break;
                    iatIndex++;
                    bool isOrdinal = (iltEntry & 0x8000000000000000) != 0;
                    if (isOrdinal)
                        continue;

                    uint hintNameRva = (uint)(iltEntry & 0x7FFFFFFFFFFFFFFF);
                    var hintNameReader = File.CreateReaderAtRva(hintNameRva);
                    ushort hint = hintNameReader.ReadUInt16();
                    string functionName = hintNameReader.ReadAsciiString();
                    iatIndices[functionName] = iatIndex - 1;
                }
                iatCount = iatIndex;
            }

            // Create the PEHeaderContext
            var context = new PEHeaderContext(File, iatRva, iatCount);

            // Create the RTIH section
            {
                PESection section = new(
                    SectionRTIH,
                    SectionFlags.ContentInitializedData | SectionFlags.MemoryRead);
                using var ms = new MemoryStream();
                var writer = new BinaryWriter(ms);

                // Create the PEImporterHeader
                var header = new PEImporterHeader() {
                    Format = HeaderFormat.PE,
                    Version = 1,
                    Symbols = Symbols
                };

                // Write the entire header
                header.Write(context, writer);

                // Finalize the section
                section.Contents = new DataSegment(ms.ToArray());
                File.Sections.Add(section);
            }

            // Create new import descriptor table section
            {
                PESection section = new(
                    SectionIDNEW,
                    SectionFlags.ContentInitializedData | SectionFlags.MemoryRead);
                
                using var ms = new MemoryStream();
                var writer = new BinaryStreamWriter(ms);

                // Write original import directory RVA and size at the start
                writer.WriteUInt32(importDirectory.VirtualAddress);
                writer.WriteUInt32(importDirectory.Size);

                foreach (var descriptor in importDescriptors) {
                    if (descriptor.Equals(minecraftWindowsImportDescriptor.Value))
                        continue;
                    descriptor.Write(writer);
                }

                // Write null descriptor at the end
                ImportDescriptor.Empty.Write(writer);
                section.Contents = new DataSegment(ms.ToArray());
                File.Sections.Add(section);
                File.AlignSections();

                // Update import directory to point to the new table
                File.OptionalHeader.SetDataDirectory(
                    DataDirectoryIndex.ImportDirectory,
                    new(section.Rva + sizeof(uint) * 2, (uint)ms.Length));
                Logger.Debug("Removed import from 'Minecraft.Windows.exe'.");
            }
            Logger.Debug("Patching completed.");
            return true;
        }

        public bool RemoveSection(string name) {
            var section = File.Sections.FirstOrDefault(s => s.Name == name);
            if (section is not null) {
                return File.Sections.Remove(section);
            }
            return false;
        }

        public void Unpatch() {
            if (!IsPatched())
                return;
            Logger.Debug("Unpatching PE file...");
            for (int i = File.Sections.Count - 1; i >= 0; i--) {
                var section = File.Sections[i];
                if (section.Name == SectionIDNEW) {
                    BinaryStreamReader reader = section.CreateReader();
                    uint originalIDTRva = reader.ReadUInt32();
                    uint originalIDTSize = reader.ReadUInt32();

                    File.OptionalHeader.SetDataDirectory(
                        DataDirectoryIndex.ImportDirectory,
                        new(originalIDTRva, originalIDTSize)
                    );
                }

                if (IsCustomSection(section.Name)) {
                    Logger.Debug($"Removing custom section '{section.Name}'...");
                    File.Sections.RemoveAt(i);
                }
            }
            File.AlignSections();
        }
    }
}
