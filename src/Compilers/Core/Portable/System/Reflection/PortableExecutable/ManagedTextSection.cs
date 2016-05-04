// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

#if SRM
using System.Reflection.Internal;
using BitArithmeticUtilities = System.Reflection.Internal.BitArithmetic;
#else
using System.Reflection.PortableExecutable;
using Roslyn.Utilities;
#endif

#if SRM
namespace System.Reflection.PortableExecutable
#else
namespace Roslyn.Reflection.PortableExecutable
#endif
{
    /// <summary>
    /// Managed .text PE section.
    /// </summary>
    /// <remarks>
    /// Contains in the following order:
    /// - Import Address Table
    /// - COR Header
    /// - IL
    /// - Metadata
    /// - Managed Resource Data
    /// - Strong Name Signature
    /// - Debug Table
    /// - Import Table
    /// - Name Table
    /// - Runtime Startup Stub
    /// - Mapped Field Data
    /// </remarks>
#if SRM
    public
#endif
    sealed class ManagedTextSection
    {
        public Characteristics ImageCharacteristics { get; }
        public Machine Machine { get; }
        public bool IsDeterministic { get; }
        public string PdbPathOpt { get; }

        /// <summary>
        /// Total size of metadata (header and all streams).
        /// </summary>
        public int MetadataSize { get; }

        /// <summary>
        /// The size of IL stream (unaligned).
        /// </summary>
        public int ILStreamSize { get; }

        /// <summary>
        /// The size of mapped field data stream.
        /// Aligned to <see cref="MappedFieldDataAlignment"/>.
        /// </summary>
        public int MappedFieldDataSize { get; }

        /// <summary>
        /// The size of managed resource data stream.
        /// Aligned to <see cref="ManagedResourcesDataAlignment"/>.
        /// </summary>
        public int ResourceDataSize { get; }

        /// <summary>
        /// Size of strong name hash.
        /// </summary>
        public int StrongNameSignatureSize { get; }

        public ManagedTextSection(
            int metadataSize,
            int ilStreamSize,
            int mappedFieldDataSize,
            int resourceDataSize,
            int strongNameSignatureSize,
            Characteristics imageCharacteristics,
            Machine machine, 
            string pdbPathOpt,
            bool isDeterministic)
        {
            MetadataSize = metadataSize;
            ResourceDataSize = resourceDataSize;
            ILStreamSize = ilStreamSize;
            MappedFieldDataSize = mappedFieldDataSize;
            StrongNameSignatureSize = strongNameSignatureSize;
            ImageCharacteristics = imageCharacteristics;
            Machine = machine;
            PdbPathOpt = pdbPathOpt;
            IsDeterministic = isDeterministic;
        }

        /// <summary>
        /// If set, the module must include a machine code stub that transfers control to the virtual execution system.
        /// </summary>
        internal bool RequiresStartupStub => Machine == Machine.I386 || Machine == 0;

        /// <summary>
        /// If set, the module contains instructions that assume a 64 bit instruction set. For example it may depend on an address being 64 bits.
        /// This may be true even if the module contains only IL instructions because of PlatformInvoke and COM interop.
        /// </summary>
        internal bool Requires64bits => Machine == Machine.Amd64 || Machine == Machine.IA64;

        public bool Is32Bit => !Requires64bits;

        public const int ManagedResourcesDataAlignment = 8;

        private const string CorEntryPointDll = "mscoree.dll";
        private string CorEntryPointName => (ImageCharacteristics & Characteristics.Dll) != 0 ? "_CorDllMain" : "_CorExeMain";

        private int SizeOfImportAddressTable => RequiresStartupStub ? (Is32Bit ? 2 * sizeof(uint) : 2 * sizeof(ulong)) : 0;

        // (_is32bit ? 66 : 70);
        private int SizeOfImportTable =>
            sizeof(uint) + // RVA
            sizeof(uint) + // 0           
            sizeof(uint) + // 0
            sizeof(uint) + // name RVA
            sizeof(uint) + // import address table RVA
            20 +           // ?
            (Is32Bit ? 3 * sizeof(uint) : 2 * sizeof(ulong)) + // import lookup table
            sizeof(ushort) + // hint
            CorEntryPointName.Length +
            1;    // NUL

        private static int SizeOfNameTable =>
            CorEntryPointDll.Length + 1 + sizeof(ushort);

        private int SizeOfRuntimeStartupStub => Is32Bit ? 8 : 16;

        public const int MappedFieldDataAlignment = 8;

        public int CalculateOffsetToMappedFieldDataStream()
        {
            int result = ComputeOffsetToImportTable();

            if (RequiresStartupStub)
            {
                result += SizeOfImportTable + SizeOfNameTable;
                result = BitArithmeticUtilities.Align(result, Is32Bit ? 4 : 8); //optional padding to make startup stub's target address align on word or double word boundary
                result += SizeOfRuntimeStartupStub;
            }

            return result;
        }

        private int ComputeOffsetToDebugTable()
        {
            Debug.Assert(MetadataSize % 4 == 0);
            Debug.Assert(ResourceDataSize % 4 == 0);

            return
                ComputeOffsetToMetadata() +
                MetadataSize +
                ResourceDataSize +
                StrongNameSignatureSize;
        }

        private int ComputeOffsetToImportTable()
        {
            return
                ComputeOffsetToDebugTable() +
                ComputeSizeOfDebugDirectory();
        }

        private const int CorHeaderSize =
            sizeof(int) +    // header size
            sizeof(short) +  // major runtime version
            sizeof(short) +  // minor runtime version
            sizeof(long) +   // metadata directory
            sizeof(int) +    // COR flags
            sizeof(int) +    // entry point
            sizeof(long) +   // resources directory
            sizeof(long) +   // strong name signature directory
            sizeof(long) +   // code manager table directory
            sizeof(long) +   // vtable fixups directory
            sizeof(long) +   // export address table jumps directory
            sizeof(long);   // managed-native header directory

        public int OffsetToILStream => SizeOfImportAddressTable + CorHeaderSize;

        private int ComputeOffsetToMetadata()
        {
            return OffsetToILStream + BitArithmeticUtilities.Align(ILStreamSize, 4);
        }

        /// <summary>
        /// The size of a single entry in the "Debug Directory (Image Only)"
        /// </summary>
        private const int ImageDebugDirectoryEntrySize =
            sizeof(uint) +   // Characteristics
            sizeof(uint) +   // TimeDataStamp
            sizeof(uint) +   // Version
            sizeof(uint) +   // Type
            sizeof(uint) +   // SizeOfData
            sizeof(uint) +   // AddressOfRawData
            sizeof(uint);    // PointerToRawData

        private bool EmitPdb => PdbPathOpt != null;

        /// <summary>
        /// Minimal size of PDB path in Debug Directory. We pad the path to this minimal size to
        /// allow some tools to patch the path without the need to rewrite the entire image.
        /// This is a workaround put in place until these tools are retired.
        /// </summary>
        private int MinPdbPath => IsDeterministic ? 0 : 260;

        /// <summary>
        /// The size of our debug directory: one entry for debug information, and an optional second one indicating
        /// that the timestamp is deterministic (i.e. not really a timestamp)
        /// </summary>
        private int ImageDebugDirectoryBaseSize =>
            (IsDeterministic ? ImageDebugDirectoryEntrySize : 0) +
            (EmitPdb ? ImageDebugDirectoryEntrySize : 0);

        private int ComputeSizeOfDebugDirectoryData()
        {
            // The debug directory data is only needed if this.EmitPdb.
            return (!EmitPdb) ? 0 :
                4 +              // 4B signature "RSDS"
                16 +             // GUID
                sizeof(uint) +   // Age
                Math.Max(BlobUtilities.GetUTF8ByteCount(PdbPathOpt) + 1, MinPdbPath);
        }

        private int ComputeSizeOfDebugDirectory()
        {
            return ImageDebugDirectoryBaseSize + ComputeSizeOfDebugDirectoryData();
        }

        public DirectoryEntry GetDebugDirectoryEntry(int rva)
        {
            // Only the size of the fixed part of the debug table goes here.
            return (EmitPdb || IsDeterministic) ?
                new DirectoryEntry(rva + ComputeOffsetToDebugTable(), ImageDebugDirectoryBaseSize) :
                default(DirectoryEntry);
        }

        public int ComputeSizeOfTextSection()
        {
            Debug.Assert(MappedFieldDataSize % MappedFieldDataAlignment == 0);
            return CalculateOffsetToMappedFieldDataStream() + MappedFieldDataSize;
        }

        public int GetEntryPointAddress(int rva)
        {
            // TODO: constants
            return RequiresStartupStub ?
                rva + CalculateOffsetToMappedFieldDataStream() - (Is32Bit ? 6 : 10) :
                0;
        }

        public DirectoryEntry GetImportAddressTableDirectoryEntry(int rva)
        {
            return RequiresStartupStub ?
                new DirectoryEntry(rva, SizeOfImportAddressTable) :
                default(DirectoryEntry);
        }

        public DirectoryEntry GetImportTableDirectoryEntry(int rva)
        {
            // TODO: constants
            return RequiresStartupStub ?
                new DirectoryEntry(rva + ComputeOffsetToImportTable(), (Is32Bit ? 66 : 70) + 13) :
                default(DirectoryEntry);
        }

        public DirectoryEntry GetCorHeaderDirectoryEntry(int rva)
        {
            return new DirectoryEntry(rva + SizeOfImportAddressTable, CorHeaderSize);
        }

        #region Serialization

        /// <summary>
        /// Serializes .text section data into a specified <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">An empty builder to serialize section data to.</param>
        /// <param name="relativeVirtualAddess">Relative virtual address of the section within the containing PE file.</param>
        /// <param name="entryPointTokenOrRelativeVirtualAddress">Entry point token or RVA (<see cref="CorHeader.EntryPointTokenOrRelativeVirtualAddress"/>)</param>
        /// <param name="corFlags">COR Flags (<see cref="CorHeader.Flags"/>).</param>
        /// <param name="baseAddress">Base address of the PE image.</param>
        /// <param name="metadataBuilder"><see cref="BlobBuilder"/> containing metadata. Must be populated with data. Linked into the <paramref name="builder"/> and can't be expanded afterwards.</param>
        /// <param name="ilBuilder"><see cref="BlobBuilder"/> containing IL stream. Must be populated with data. Linked into the <paramref name="builder"/> and can't be expanded afterwards.</param>
        /// <param name="mappedFieldDataBuilder"><see cref="BlobBuilder"/> containing mapped field data. Must be populated with data. Linked into the <paramref name="builder"/> and can't be expanded afterwards.</param>
        /// <param name="resourceBuilder"><see cref="BlobBuilder"/> containing managed resource data. Must be populated with data. Linked into the <paramref name="builder"/> and can't be expanded afterwards.</param>
        /// <param name="debugTableBuilderOpt"><see cref="BlobBuilder"/> containing debug table data. Must be populated with data. Linked into the <paramref name="builder"/> and can't be expanded afterwards.</param>
        public void Serialize(
            BlobBuilder builder,
            int relativeVirtualAddess,
            int entryPointTokenOrRelativeVirtualAddress,
            CorFlags corFlags,
            ulong baseAddress,
            BlobBuilder metadataBuilder,
            BlobBuilder ilBuilder,
            BlobBuilder mappedFieldDataBuilder,
            BlobBuilder resourceBuilder,
            BlobBuilder debugTableBuilderOpt)
        {
            Debug.Assert(builder.Count == 0);
            Debug.Assert(metadataBuilder.Count == MetadataSize);
            Debug.Assert(metadataBuilder.Count % 4 == 0);
            Debug.Assert(ilBuilder.Count == ILStreamSize);
            Debug.Assert(mappedFieldDataBuilder.Count == MappedFieldDataSize);
            Debug.Assert(resourceBuilder.Count == ResourceDataSize);
            Debug.Assert(resourceBuilder.Count % 4 == 0);

            // TODO: avoid recalculation
            int importTableRva = GetImportTableDirectoryEntry(relativeVirtualAddess).RelativeVirtualAddress;
            int importAddressTableRva = GetImportAddressTableDirectoryEntry(relativeVirtualAddess).RelativeVirtualAddress;

            if (RequiresStartupStub)
            {
                WriteImportAddressTable(builder, importTableRva);
            }

            WriteCorHeader(builder, relativeVirtualAddess, entryPointTokenOrRelativeVirtualAddress, corFlags);

            // IL:
            ilBuilder.Align(4);
            builder.LinkSuffix(ilBuilder);

            // metadata:
            builder.LinkSuffix(metadataBuilder);

            // managed resources:
            builder.LinkSuffix(resourceBuilder);

            // strong name signature:
            builder.WriteBytes(0, StrongNameSignatureSize);

            if (debugTableBuilderOpt != null)
            {
                builder.LinkSuffix(debugTableBuilderOpt);
            }

            if (RequiresStartupStub)
            {
                WriteImportTable(builder, importTableRva, importAddressTableRva);
                WriteNameTable(builder);
                WriteRuntimeStartupStub(builder, importAddressTableRva, baseAddress);
            }

            // mapped field data:            
            builder.LinkSuffix(mappedFieldDataBuilder);

            Debug.Assert(builder.Count == ComputeSizeOfTextSection());
        }

        private void WriteImportAddressTable(BlobBuilder builder, int importTableRva)
        {
            int start = builder.Count;
            
            int ilRva = importTableRva + 40;
            int hintRva = ilRva + (Is32Bit ? 12 : 16);

            // Import Address Table
            if (Is32Bit)
            {
                builder.WriteUInt32((uint)hintRva); // 4
                builder.WriteUInt32(0); // 8
            }
            else
            {
                builder.WriteUInt64((uint)hintRva); // 8
                builder.WriteUInt64(0); // 16
            }

            Debug.Assert(builder.Count - start == SizeOfImportAddressTable);
        }

        private void WriteImportTable(BlobBuilder builder, int importTableRva, int importAddressTableRva)
        {
            int start = builder.Count;

            int ilRVA = importTableRva + 40;
            int hintRva = ilRVA + (Is32Bit ? 12 : 16);
            int nameRva = hintRva + 12 + 2;

            // Import table
            builder.WriteUInt32((uint)ilRVA); // 4
            builder.WriteUInt32(0); // 8
            builder.WriteUInt32(0); // 12
            builder.WriteUInt32((uint)nameRva); // 16
            builder.WriteUInt32((uint)importAddressTableRva); // 20
            builder.WriteBytes(0, 20); // 40

            // Import Lookup table
            if (Is32Bit)
            {
                builder.WriteUInt32((uint)hintRva); // 44
                builder.WriteUInt32(0); // 48
                builder.WriteUInt32(0); // 52
            }
            else
            {
                builder.WriteUInt64((uint)hintRva); // 48
                builder.WriteUInt64(0); // 56
            }

            // Hint table
            builder.WriteUInt16(0); // Hint 54|58
            
            foreach (char ch in CorEntryPointName)
            {
                builder.WriteByte((byte)ch); // 65|69
            }

            builder.WriteByte(0); // 66|70
            Debug.Assert(builder.Count - start == SizeOfImportTable);
        }

        private static void WriteNameTable(BlobBuilder builder)
        {
            int start = builder.Count;

            foreach (char ch in CorEntryPointDll)
            {
                builder.WriteByte((byte)ch);
            }

            builder.WriteByte(0);
            builder.WriteUInt16(0);
            Debug.Assert(builder.Count - start == SizeOfNameTable);
        }

        private void WriteCorHeader(BlobBuilder builder, int textSectionRva, int entryPointTokenOrRva, CorFlags corFlags)
        {
            const ushort majorRuntimeVersion = 2;
            const ushort minorRuntimeVersion = 5;

            int metadataRva = textSectionRva + ComputeOffsetToMetadata();
            int resourcesRva = metadataRva + MetadataSize;
            int signatureRva = resourcesRva + ResourceDataSize;

            int start = builder.Count;

            // Size:
            builder.WriteUInt32(CorHeaderSize);

            // Version:
            builder.WriteUInt16(majorRuntimeVersion);
            builder.WriteUInt16(minorRuntimeVersion);

            // MetadataDirectory:
            builder.WriteUInt32((uint)metadataRva);
            builder.WriteUInt32((uint)MetadataSize);

            // COR Flags:
            builder.WriteUInt32((uint)corFlags);

            // EntryPoint:
            builder.WriteUInt32((uint)entryPointTokenOrRva);

            // ResourcesDirectory:
            builder.WriteUInt32((uint)(ResourceDataSize == 0 ? 0 : resourcesRva)); // 28
            builder.WriteUInt32((uint)ResourceDataSize);

            // StrongNameSignatureDirectory:
            builder.WriteUInt32((uint)(StrongNameSignatureSize == 0 ? 0 : signatureRva)); // 36
            builder.WriteUInt32((uint)StrongNameSignatureSize);

            // CodeManagerTableDirectory (not supported):
            builder.WriteUInt32(0);
            builder.WriteUInt32(0);

            // VtableFixupsDirectory (not supported):
            builder.WriteUInt32(0);
            builder.WriteUInt32(0);

            // ExportAddressTableJumpsDirectory (not supported):
            builder.WriteUInt32(0);
            builder.WriteUInt32(0);

            // ManagedNativeHeaderDirectory (not supported):
            builder.WriteUInt32(0);
            builder.WriteUInt32(0);

            Debug.Assert(builder.Count - start == CorHeaderSize);
            Debug.Assert(builder.Count % 4 == 0);
        }

        /// <summary>
        /// Write one entry in the "Debug Directory (Image Only)"
        /// See https://msdn.microsoft.com/en-us/windows/hardware/gg463119.aspx
        /// section 5.1.1 (pages 71-72).
        /// </summary>
        private static void WriteDebugTableEntry(
            BlobBuilder writer,
            byte[] stamp,
            uint version, // major and minor version, combined
            uint debugType,
            uint sizeOfData,
            uint addressOfRawData,
            uint pointerToRawData)
        {
            writer.WriteUInt32(0); // characteristics
            Debug.Assert(stamp.Length == 4);
            writer.WriteBytes(stamp);
            writer.WriteUInt32(version);
            writer.WriteUInt32(debugType);
            writer.WriteUInt32(sizeOfData);
            writer.WriteUInt32(addressOfRawData);
            writer.WriteUInt32(pointerToRawData);
        }

        private readonly static byte[] zeroStamp = new byte[4]; // four bytes of zero

        /// <summary>
        /// Write the entire "Debug Directory (Image Only)" along with data that it points to.
        /// </summary>
        internal void WriteDebugTable(BlobBuilder builder, PESectionLocation textSectionLocation, ContentId nativePdbContentId, ContentId portablePdbContentId)
        {
            Debug.Assert(builder.Count == 0);

            int tableSize = ImageDebugDirectoryBaseSize;
            Debug.Assert(tableSize != 0);
            Debug.Assert(nativePdbContentId.IsDefault || portablePdbContentId.IsDefault);
            Debug.Assert(!EmitPdb || (nativePdbContentId.IsDefault ^ portablePdbContentId.IsDefault));

            int dataSize = ComputeSizeOfDebugDirectoryData();
            if (EmitPdb)
            {
                const int IMAGE_DEBUG_TYPE_CODEVIEW = 2; // from PE spec
                uint dataOffset = (uint)(ComputeOffsetToDebugTable() + tableSize);
                WriteDebugTableEntry(builder,
                    stamp: nativePdbContentId.Stamp ?? portablePdbContentId.Stamp,
                    version: portablePdbContentId.IsDefault ? (uint)0 : ('P' << 24 | 'M' << 16 | 0x01 << 8 | 0x00),
                    debugType: IMAGE_DEBUG_TYPE_CODEVIEW,
                    sizeOfData: (uint)dataSize,
                    addressOfRawData: (uint)textSectionLocation.RelativeVirtualAddress + dataOffset, // RVA of the data
                    pointerToRawData: (uint)textSectionLocation.PointerToRawData + dataOffset); // position of the data in the PE stream
            }

            if (IsDeterministic)
            {
                const int IMAGE_DEBUG_TYPE_NO_TIMESTAMP = 16; // from PE spec
                WriteDebugTableEntry(builder,
                    stamp: zeroStamp,
                    version: 0,
                    debugType: IMAGE_DEBUG_TYPE_NO_TIMESTAMP,
                    sizeOfData: 0,
                    addressOfRawData: 0,
                    pointerToRawData: 0);
            }

            // We should now have written all and precisely the data we said we'd write for the table entries.
            Debug.Assert(builder.Count == tableSize);

            // ====================
            // The following is additional data beyond the debug directory at the offset `dataOffset`
            // pointed to by the ImageDebugTypeCodeView entry.

            if (EmitPdb)
            {
                builder.WriteByte((byte)'R');
                builder.WriteByte((byte)'S');
                builder.WriteByte((byte)'D');
                builder.WriteByte((byte)'S');

                // PDB id:
                builder.WriteBytes(nativePdbContentId.Guid ?? portablePdbContentId.Guid);

                // age
                builder.WriteUInt32(1); // TODO: allow specify for native PDBs

                // UTF-8 encoded zero-terminated path to PDB
                int pathStart = builder.Count;
                builder.WriteUTF8(PdbPathOpt, allowUnpairedSurrogates: true);
                builder.WriteByte(0);

                // padding:
                builder.WriteBytes(0, Math.Max(0, MinPdbPath - (builder.Count - pathStart)));
            }

            // We should now have written all and precisely the data we said we'd write for the table and its data.
            Debug.Assert(builder.Count == tableSize + dataSize);
        }

        private void WriteRuntimeStartupStub(BlobBuilder sectionBuilder, int importAddressTableRva, ulong baseAddress)
        {
            // entry point code, consisting of a jump indirect to _CorXXXMain
            if (Is32Bit)
            {
                // Write zeros (nops) to pad the entry point code so that the target address is aligned on a 4 byte boundary.
                // Note that the section is aligned to FileAlignment, which is at least 512, so we can align relatively to the start of the section.
                sectionBuilder.Align(4);

                sectionBuilder.WriteUInt16(0);
                sectionBuilder.WriteByte(0xff);
                sectionBuilder.WriteByte(0x25); //4
                sectionBuilder.WriteUInt32((uint)importAddressTableRva + (uint)baseAddress); //8
            }
            else
            {
                // Write zeros (nops) to pad the entry point code so that the target address is aligned on a 8 byte boundary.
                // Note that the section is aligned to FileAlignment, which is at least 512, so we can align relatively to the start of the section.
                sectionBuilder.Align(8);

                sectionBuilder.WriteUInt32(0);
                sectionBuilder.WriteUInt16(0);
                sectionBuilder.WriteByte(0xff);
                sectionBuilder.WriteByte(0x25); //8
                sectionBuilder.WriteUInt64((ulong)importAddressTableRva + baseAddress); //16
            }
        }

        #endregion
    }
}