using Amethyst.Common.Diagnostics;
using LibObjectFile.Diagnostics;
using LibObjectFile.PE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching.PE {
    public class PEPatcher(PEFile file, List<ImportedSymbol> symbols) : IPatcher {
        // Runtime Importer Header
        public const string SectionRTIH = ".rtih";

        public static HashSet<string> CustomSections { get; } = [
            SectionRTIH
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

        static uint AlignSectionRVA(uint value, uint align) => (value + align - 1) & ~(align - 1);

        public bool Patch() {
            // Version 1 full PE-specific layout:
            // .rtih - Runtime Importer Header
            // [PEImporterHeader data]

            if (IsPatched()) {
                Logger.Debug("PE file is already patched, no reason to repatch.");
                return false;
            }

            Logger.Debug("Patching PE file for runtime importing...");

            // Get the import directory entries
            var importDirectory = File.Directories.Import;
            if (importDirectory is null) {
                Logger.Warn("PE file has no import directory, no reason to patch it.");
                return false;
            }

            // Find the import directory entry for "Minecraft.Windows.exe"
            PEImportDirectoryEntry? targetImportDirectoryEntry = importDirectory.Entries.FirstOrDefault(e => e.ImportDllNameLink.Resolve()?.StartsWith("Minecraft.Windows", StringComparison.OrdinalIgnoreCase) ?? false);

            // If not found, no reason to patch
            if (targetImportDirectoryEntry is null) {
                Logger.Warn("PE file does not import from 'Minecraft.Windows.exe', no reason to patch it.");
                return false;
            }

            // Map all mangled names for "Minecraft.Windows.exe" functions to IAT offsets
            uint iatRva = targetImportDirectoryEntry.ImportAddressTable.RVA;
            uint iatCount = 0;
            Dictionary<string, uint> iatIndices = [];
            foreach (var import in targetImportDirectoryEntry.ImportLookupTable.Entries) {
                iatCount++;
                if (import.IsImportByOrdinal)
                    continue;
                var name = import.HintName.Resolve().Name;
                if (name is not null) {
                    iatIndices[name] = iatCount - 1;
                }
            }

            // Create the PEHeaderContext
            var context = new PEHeaderContext(File, iatRva, iatCount);
            var lastSection = File.Sections[^1]!;
            var sectionAlignment = File.OptionalHeader.SectionAlignment;
            var fileAlignment = File.OptionalHeader.FileAlignment;
            var nextRVA = AlignSectionRVA(lastSection.RVA + lastSection.VirtualSize, sectionAlignment);

            // Create the RTIH section
            PESection rtihSection = File.AddSection(SectionRTIH, nextRVA);
            rtihSection.Characteristics = SectionCharacteristics.ContainsInitializedData | SectionCharacteristics.MemRead;
            var rthiStream = new PEStreamSectionData();

            var header = new PEImporterHeader() {
                Format = HeaderFormat.PE,
                Version = 1,
                Symbols = Symbols
            };
            using var writer = new BinaryWriter(rthiStream.Stream, Encoding.UTF8, true);
            header.Write(context, writer);
            rtihSection.Content.Add(rthiStream);

            // Remove the target import directory entry from the import directory
            importDirectory.Entries.Remove(targetImportDirectoryEntry);
            DiagnosticBag diags = new() { EnableStackTrace = true };
            File.Verify(diags);
            foreach (var diag in diags.Messages) {
                switch (diag.Kind) {
                    case DiagnosticKind.Error:
                        Logger.Error(diag.ToString());
                        break;
                    case DiagnosticKind.Warning:
                        Logger.Warn(diag.ToString());
                        break;
                    default:
                        Logger.Info(diag.ToString());
                        break;
                }
            }
            Logger.Debug("Removed import from 'Minecraft.Windows.exe'.");
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
    }
}
