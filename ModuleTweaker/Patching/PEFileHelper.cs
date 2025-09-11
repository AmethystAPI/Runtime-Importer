using Amethyst.Common.Diagnostics;
using Amethyst.Common.Models;
using Amethyst.ModuleTweaker.Patching.Models;
using AsmResolver;
using AsmResolver.IO;
using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching
{
    public class PEFileHelper(PEFile file)
    {
        public const string StringTableName = ".strt";
        public const string FunctionDescriptorSectionName = ".fndt";
        public const string VirtualFunctionDescriptorSectionName = ".vfndt";
        public const string VariableDescriptorSectionName = ".vardt";
        public const string VirtualTableDescriptorSectionName = ".vtbdt";
        public const string NewImportDescriptorSectionName = ".idnew";
        public static HashSet<string> SectionNames = [
            VariableDescriptorSectionName,
            FunctionDescriptorSectionName,
            StringTableName,
            VirtualFunctionDescriptorSectionName,
            VirtualTableDescriptorSectionName,
            NewImportDescriptorSectionName
        ];

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
                if (section.Name == NewImportDescriptorSectionName)
                {
                    BinaryStreamReader reader = section.CreateReader();
                    uint originalIDTRva = reader.ReadUInt32();
                    uint originalIDTSize = reader.ReadUInt32();

                    File.OptionalHeader.SetDataDirectory(
                        DataDirectoryIndex.ImportDirectory,
                        new(originalIDTRva, originalIDTSize)
                    );
                }

                if (IsCustomSectionName(section.Name))
                {
                    Logger.Info($"Removing custom section '{section.Name}'...");
                    File.Sections.RemoveAt(i);
                }
            }
            File.AlignSections();
        }

        public bool Patch(
            IEnumerable<FunctionSymbolModel> functions, 
            IEnumerable<VariableSymbolModel> variables, 
            IEnumerable<VirtualTableSymbolModel> vtables,
            IEnumerable<VirtualFunctionSymbolModel> vfunctions)
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
            uint iatRva = minecraftWindowsImportDescriptor.Value.FirstThunk;
            uint iatSize = 0;
            Dictionary<string, uint> iatIndices = [];
            {
                var reader = File.CreateReaderAtRva(minecraftWindowsImportDescriptor.Value.OriginalFirstThunk);
                uint iatIndex = 0;
                while (reader.CanRead(sizeof(ulong)))
                {
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
                iatSize = iatIndex;
            }

            Dictionary<string, uint> stringToIndex = [];
            {
                // Create string table section
                // Contains all strings (eg. mangled names, signatures) to be resolved at runtime
                PESection section = new(
                    StringTableName,
                    SectionFlags.ContentInitializedData | SectionFlags.MemoryRead);

                using var ms = new MemoryStream();
                var writer = new BinaryStreamWriter(ms);
                uint countPosition = (uint)ms.Position;
                writer.WriteUInt32(0u); // Placeholder for count
                uint index = 0;

                // Write all function names
                foreach (var method in functions)
                {
                    writer.WriteBytes([.. Encoding.ASCII.GetBytes(method.Name), 0]);
                    stringToIndex[method.Name] = index++;
                    
                }

                // Write all function signatures
                foreach (var method in functions.Where(m => m.Signature is not null))
                {
                    writer.WriteBytes([.. Encoding.ASCII.GetBytes(method.Signature!), 0]);
                    stringToIndex[method.Signature!] = index++;
                }

                // Write all variable names
                foreach (var variable in variables)
                {
                    writer.WriteBytes([.. Encoding.ASCII.GetBytes(variable.Name), 0]);
                    stringToIndex[variable.Name] = index++;
                }

                // Write all virtual table names
                foreach (var vtable in vtables)
                {
                    writer.WriteBytes([.. Encoding.ASCII.GetBytes(vtable.Name), 0]);
                    stringToIndex[vtable.Name] = index++;
                }

                // Write all virtual function names
                foreach (var vfunc in vfunctions)
                {
                    writer.WriteBytes([.. Encoding.ASCII.GetBytes(vfunc.Name), 0]);
                    stringToIndex[vfunc.Name] = index++;
                }

                // Go back and write the count
                ms.Seek(countPosition, SeekOrigin.Begin);
                writer.WriteUInt32(index);

                section.Contents = new DataSegment(ms.ToArray());
                File.Sections.Add(section);
                Logger.Info($"Added {index} strings to the string table.");
            }

            // Create function descriptor table section
            // Contains descriptors for all functions to be resolved at runtime
            {
                PESection section = new(
                    FunctionDescriptorSectionName,
                    SectionFlags.ContentInitializedData | SectionFlags.MemoryRead);
                using var ms = new MemoryStream();
                var writer = new BinaryStreamWriter(ms);
                uint countPosition = (uint)ms.Position;
                writer.WriteUInt32(0u); // Placeholder for count
                writer.WriteUInt32(iatRva);
                writer.WriteUInt32(iatSize);
                uint count = 0;
                foreach (var function in functions)
                {
                    if (!iatIndices.TryGetValue(function.Name, out uint iatIndex))
                        continue;
                    uint nameIndex = stringToIndex[function.Name];
                    bool usesSignature = function.Signature is not null;

                    writer.WriteUInt32(nameIndex);
                    writer.WriteUInt32(iatIndex);
                    writer.WriteByte((byte)(usesSignature ? 1 : 0));

                    if (usesSignature)
                    {
                        uint signatureIndex = stringToIndex[function.Signature!];
                        writer.WriteUInt64(signatureIndex);
                    }
                    else
                    {
                        if (!ulong.TryParse(function.Address?.Replace("0x", "") ?? "0", out ulong address))
                            writer.WriteUInt64(0x0);
                        else
                            writer.WriteUInt64(address);
                    }

                    // uint: NameIndex
                    // uint: IATIndex
                    // byte: UsesSignature
                    // ulong: SignatureIndex or Address
                    count++;
                    Logger.Info($"Added function: " + function.Name);
                }

                // Go back and write the count
                ms.Seek(countPosition, SeekOrigin.Begin);
                writer.WriteUInt32(count);

                section.Contents = new DataSegment(ms.ToArray());
                File.Sections.Add(section);
            }

            // Create variable descriptor table section
            // Contains descriptors for all variables to be resolved at runtime
            {
                PESection section = new(
                    VariableDescriptorSectionName,
                    SectionFlags.ContentInitializedData | SectionFlags.MemoryRead);
                using var ms = new MemoryStream();
                var writer = new BinaryStreamWriter(ms);
                uint countPosition = (uint)ms.Position;
                writer.WriteUInt32(0u); // Placeholder for count
                writer.WriteUInt32(iatRva);
                writer.WriteUInt32(iatSize);
                uint count = 0;
                foreach (var variable in variables)
                {
                    if (!iatIndices.TryGetValue(variable.Name, out uint iatIndex))
                        continue;
                    uint nameIndex = stringToIndex[variable.Name];

                    writer.WriteUInt32(nameIndex);
                    writer.WriteUInt32(iatIndex);
                    string address = variable.Address.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? variable.Address[2..] : variable.Address;
                    
                    try
                    {
                        ulong addressValue = ulong.Parse(address, NumberStyles.HexNumber);
                        writer.WriteUInt64(addressValue);
                    }
                    catch (Exception)
                    {
                        writer.WriteUInt64(0x0);
                    }

                    // uint: NameIndex
                    // uint: IATIndex
                    // ulong: Address
                    count++;
                    Logger.Info($"Added variable: " + variable.Name);
                }

                // Go back and write the count
                ms.Seek(countPosition, SeekOrigin.Begin);
                writer.WriteUInt32(count);

                section.Contents = new DataSegment(ms.ToArray());
                File.Sections.Add(section);
            }

            // Create virtual table descriptor table section
            {
                PESection section = new(
                    VirtualTableDescriptorSectionName,
                    SectionFlags.ContentInitializedData | SectionFlags.MemoryRead);
                using var ms = new MemoryStream();
                var writer = new BinaryStreamWriter(ms);
                uint countPosition = (uint)ms.Position;
                writer.WriteUInt32(0u); // Placeholder for count
                uint count = 0;
                foreach (var vtable in vtables)
                {
                    uint nameIndex = stringToIndex[vtable.Name];

                    writer.WriteUInt32(nameIndex);
                    string address = vtable.Address.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? vtable.Address[2..] : vtable.Address;
                    
                    try
                    {
                        ulong addressValue = ulong.Parse(address, NumberStyles.HexNumber);
                        writer.WriteUInt64(addressValue);
                    }
                    catch (Exception)
                    {
                        writer.WriteUInt64(0x0);
                    }

                    // uint: NameIndex
                    // ulong: Address
                    count++;
                    Logger.Info($"Added virtual table: " + vtable.Name);
                }

                // Go back and write the count
                ms.Seek(countPosition, SeekOrigin.Begin);
                writer.WriteUInt32(count);

                section.Contents = new DataSegment(ms.ToArray());
                File.Sections.Add(section);
            }

            // Create virtual function descriptor table section
            {
                PESection section = new(
                    VirtualFunctionDescriptorSectionName,
                    SectionFlags.ContentInitializedData | SectionFlags.MemoryRead);
                using var ms = new MemoryStream();
                var writer = new BinaryStreamWriter(ms);
                uint countPosition = (uint)ms.Position;
                writer.WriteUInt32(0u); // Placeholder for count
                writer.WriteUInt32(iatRva);
                writer.WriteUInt32(iatSize);
                uint count = 0;
                foreach (var vfunc in vfunctions)
                {
                    if (!iatIndices.TryGetValue(vfunc.Name, out uint iatIndex))
                        continue;
                    if (!stringToIndex.TryGetValue(vfunc.VirtualTable, out uint vtableIndex))
                        continue;

                    uint nameIndex = stringToIndex[vfunc.Name];

                    writer.WriteUInt32(nameIndex);
                    writer.WriteUInt32(iatIndex);
                    writer.WriteUInt32(vtableIndex);
                    writer.WriteUInt32(vfunc.Index);

                    // uint: NameIndex
                    // uint: IATIndex
                    // uint: VirtualTableNameIndex
                    // uint: FunctionIndex
                    count++;
                    Logger.Info($"Added virtual function: " + vfunc.Name);
                }

                // Go back and write the count
                ms.Seek(countPosition, SeekOrigin.Begin);
                writer.WriteUInt32(count);

                section.Contents = new DataSegment(ms.ToArray());
                File.Sections.Add(section);
            }

            // Create new import descriptor table section
            // Contains a copy of the original import descriptor table without the "Minecraft.Windows.exe" entry
            {
                PESection section = new(
                    NewImportDescriptorSectionName,
                    SectionFlags.ContentInitializedData | SectionFlags.MemoryRead);
                using var ms = new MemoryStream();
                var writer = new BinaryStreamWriter(ms);

                // Write original import directory RVA and size at the start
                writer.WriteUInt32(importDirectory.VirtualAddress);
                writer.WriteUInt32(importDirectory.Size);

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
                    new(section.Rva + sizeof(uint) * 2, (uint)ms.Length));
                Logger.Info("Removed import from 'Minecraft.Windows.exe'.");
            }
            Logger.Info("Patching completed.");
            return true;
        }

        public static bool IsCustomSectionName(string name) => SectionNames.Contains(name);
    }
}
