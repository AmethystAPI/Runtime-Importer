using Amethyst.Common.Diagnostics;
using Amethyst.Common.Models;
using Amethyst.ModuleTweaker.Patching.Models;
using AsmResolver;
using AsmResolver.IO;
using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching
{
    public class PEFileHelper(PEFile file)
    {
        public const string RuntimeImportFunctionDescriptorTableName = ".rifdt";
        public const string RuntimeImportMangleTableName = ".rimt";
        public const string RuntimeImportSignatureTableName = ".rist";
        public const string NewImportDescriptorTableName = ".idnew";
        public static HashSet<string> SectionNames = new()
        {
            RuntimeImportFunctionDescriptorTableName,
            RuntimeImportMangleTableName,
            RuntimeImportSignatureTableName,
            NewImportDescriptorTableName
        };

        public PEFile File { get; private set; } = file;

        public bool IsPatched()
        {
            foreach (var section in File.Sections)
            {
                if (IsCustomSectionName(section.Name))
                    return true;
            }
            return false;
        }

        public void RemoveSection(string name)
        {
            var section = File.Sections.FirstOrDefault(s => s.Name == name);
            if (section is not null)
                File.Sections.Remove(section);
        }

        public void Unpatch()
        {
            if (!IsPatched())
                return;
            Logger.Info("Unpatching PE file...");
            for (int i = File.Sections.Count - 1; i >= 0; i--)
            {
                var section = File.Sections[i];
                if (section.Name == NewImportDescriptorTableName)
                {
                    var reader = section.CreateReader();
                    uint originalIDTRva = reader.ReadUInt32();
                    uint originalIDTSize = reader.ReadUInt32();

                    File.OptionalHeader.SetDataDirectory(
                        DataDirectoryIndex.ImportDirectory,
                        new(originalIDTRva, originalIDTSize));
                }
                if (IsCustomSectionName(section.Name))
                {
                    Logger.Info($"Removing custom section '{section.Name}'...");
                    File.Sections.RemoveAt(i);
                }
            }
        }

        public bool Patch(IEnumerable<MethodSymbolJSONModel> methodSymbols)
        {
            if (IsPatched())
            {
                Logger.Info("PE file is already patched, unpatching it first.");
                Unpatch();
            }

            Logger.Info("Patching PE file for runtime importing...");

            // Get the import directory entries
            var importDirectory = File.OptionalHeader.GetDataDirectory(DataDirectoryIndex.ImportDirectory);
            if (!importDirectory.IsPresentInPE)
            {
                Logger.Warn("PE file has no import directory, no reason to patch it.");
                return false;
            }

            // Read existing import descriptors
            List<ImportDescriptor> importDescriptors = [];
            {
                var importDirectoryReader = File.CreateDataDirectoryReader(importDirectory);
                while (importDirectoryReader.CanRead(ImportDescriptor.Size))
                {
                    var descriptor = ImportDescriptor.Read(ref importDirectoryReader);
                    if (descriptor.IsZero)
                        break;
                    importDescriptors.Add(descriptor);
                }
            }

            // Find the import descriptor for "Minecraft.Windows.exe"
            ImportDescriptor? minecraftWindowsImportDescriptor = null;
            foreach (var descriptor in importDescriptors)
            {
                var nameRva = descriptor.Name;
                var name = File.CreateReaderAtRva(nameRva).ReadAsciiString();
                if (name.StartsWith("Minecraft.Windows", StringComparison.OrdinalIgnoreCase))
                {
                    minecraftWindowsImportDescriptor = descriptor;
                    break;
                }
            }

            // If not found, no reason to patch
            if (minecraftWindowsImportDescriptor is null)
            {
                Logger.Warn("PE file does not import from 'Minecraft.Windows.exe', no reason to patch it.");
                return false;
            }

            // Map all mangled names for "Minecraft.Windows.exe" functions to IAT offsets
            uint iatRva = 0;
            Dictionary<string, uint> nameToIatIndex = [];
            {
                iatRva = minecraftWindowsImportDescriptor.Value.OriginalFirstThunk;
                var reader = File.CreateReaderAtRva(minecraftWindowsImportDescriptor.Value.OriginalFirstThunk);
                uint iatIndex = 0;
                while (reader.CanRead(sizeof(ulong)))
                {
                    ulong iltEntry = reader.ReadUInt64();
                    if (iltEntry == 0)
                        break;
                    bool isOrdinal = (iltEntry & 0x8000000000000000) != 0;
                    if (isOrdinal)
                        continue;

                    uint hintNameRva = (uint)(iltEntry & 0x7FFFFFFFFFFFFFFF);
                    var hintNameReader = File.CreateReaderAtRva(hintNameRva);
                    ushort hint = hintNameReader.ReadUInt16();
                    string functionName = hintNameReader.ReadAsciiString();
                    nameToIatIndex[functionName] = iatIndex++;
                }
            }

            MethodSymbolJSONModel[] methods = [.. methodSymbols];

            // Create mangle table section
            // Contains all mangled names of functions to be resolved at runtime
            Dictionary<string, uint> nameToIndex = [];
            {
                PESection section = new(
                    RuntimeImportMangleTableName, 
                    SectionFlags.ContentInitializedData | SectionFlags.MemoryRead);

                using var ms = new MemoryStream();
                var writer = new BinaryStreamWriter(ms);
                for (uint i = 0; i < methods.Length; i++)
                {
                    var method = methods[i];
                    writer.WriteBytes([.. Encoding.ASCII.GetBytes(method.Name), 0]);
                    nameToIndex[method.Name] = i;
                }
                section.Contents = new DataSegment(ms.ToArray());
                File.Sections.Add(section);
                Logger.Info($"Created mangle table section ({RuntimeImportMangleTableName}).");
                Logger.Info($"Added {methods.Length} mangled names to mangle table.");
            }

            // Create signature table section
            // Contains signatures (eg. DE AD BE EF ? AA ? BB) of all functions to be resolved at runtime
            Dictionary<string, uint> signatureToIndex = [];
            {
                PESection section = new(
                    RuntimeImportSignatureTableName,
                    SectionFlags.ContentInitializedData | SectionFlags.MemoryRead);
                using var ms = new MemoryStream();
                var writer = new BinaryStreamWriter(ms);
                uint index = 0;
                for (uint i = 0; i < methods.Length; i++)
                {
                    var method = methods[i];
                    if (method.Signature is null)
                        continue;
                    writer.WriteBytes([.. Encoding.ASCII.GetBytes(method.Signature), 0]);
                    signatureToIndex[method.Signature] = index++;
                }
                section.Contents = new DataSegment(ms.ToArray());
                File.Sections.Add(section);
                Logger.Info($"Created signature table section ({RuntimeImportSignatureTableName}).");
                Logger.Info($"Added {signatureToIndex.Count} signatures to signature table.");
            }

            // Create function descriptor table section
            // Contains descriptors for all functions to be resolved at runtime
            {
                PESection section = new(
                    RuntimeImportFunctionDescriptorTableName,
                    SectionFlags.ContentInitializedData | SectionFlags.MemoryRead);
                using var ms = new MemoryStream();
                var writer = new BinaryStreamWriter(ms);
                uint countPosition = (uint)ms.Position;
                writer.WriteUInt32(0u); // Placeholder for count
                uint count = 0;
                for (uint i = 0; i < methods.Length; i++)
                {
                    var method = methods[i];
                    if (!nameToIatIndex.ContainsKey(method.Name))
                    {
                        Logger.Warn($"Skipping method '{method.Name}' as it does not exist in the import table of 'Minecraft.Windows.exe'.");
                        continue;
                    }

                    uint nameIndex = nameToIndex[method.Name];
                    uint iatIndex = nameToIatIndex[method.Name];
                    bool usesSignature = method.Signature is not null;

                    writer.WriteUInt32(nameIndex);
                    writer.WriteUInt32(iatIndex);
                    writer.WriteUInt32(iatRva);
                    writer.WriteByte((byte)(usesSignature ? 1 : 0));

                    if (usesSignature)
                    {
                        uint signatureIndex = signatureToIndex[method.Signature!];
                        writer.WriteUInt64(signatureIndex);
                    }
                    else
                    {
                        if (!ulong.TryParse(method.Address?.Replace("0x", "") ?? "0", out ulong address))
                            writer.WriteUInt64(0x0);
                        else
                            writer.WriteUInt64(address);
                    }

                    // uint: NameIndex
                    // uint: IATIndex
                    // uint: IATRva
                    // byte: UsesSignature
                    // ulong: SignatureIndex or Address
                    count++;
                    Logger.Info($"Added runtime import for function: " + method.Name);
                }

                // Go back and write the count
                ms.Seek(countPosition, SeekOrigin.Begin);
                writer.WriteUInt32(count);

                section.Contents = new DataSegment(ms.ToArray());
                File.Sections.Add(section);
                Logger.Info($"Created function descriptor table section ({RuntimeImportFunctionDescriptorTableName}).");
                Logger.Info($"Added {methods.Length} function descriptors to function descriptor table.");
            }

            // Create new import descriptor table section
            // Contains a copy of the original import descriptor table without the "Minecraft.Windows.exe" entry
            {
                PESection section = new(
                    NewImportDescriptorTableName,
                    SectionFlags.ContentInitializedData | SectionFlags.MemoryRead);
                using var ms = new MemoryStream();
                var writer = new BinaryStreamWriter(ms);
                foreach (var descriptor in importDescriptors)
                {
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
                    new(section.Rva, (uint)ms.Length));
                Logger.Info("Killed import from 'Minecraft.Windows.exe', patched module for runtime importing.");
            }
            File.AlignSections();
            return true;
        }

        public static bool IsCustomSectionName(string name) => SectionNames.Contains(name);
    }
}
