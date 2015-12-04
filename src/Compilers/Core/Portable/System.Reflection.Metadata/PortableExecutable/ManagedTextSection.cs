// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Utilities;

namespace System.Reflection.PortableExecutable
{
    using CciDirectoryEntry = Microsoft.Cci.DirectoryEntry;
    using CciCorHeader = Microsoft.Cci.CorHeader;
    using ContentId = Microsoft.Cci.ContentId;

    internal sealed class ManagedTextSection
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
        /// The size of IL stream.
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
                ComputeOffsetToMetadata(ILStreamSize) +
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

        private int ComputeOffsetToMetadata(int ilStreamLength)
        {
            return OffsetToILStream + BitArithmeticUtilities.Align(ilStreamLength, 4);
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

        public CciDirectoryEntry GetDebugDirectoryEntry(int rva)
        {
            return (EmitPdb || IsDeterministic) ?
                new CciDirectoryEntry(rva + ComputeOffsetToDebugTable(), ImageDebugDirectoryBaseSize) :
                default(CciDirectoryEntry);
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

        public CciDirectoryEntry GetImportAddressTableDirectoryEntry(int rva)
        {
            return RequiresStartupStub ?
                new CciDirectoryEntry(rva, SizeOfImportAddressTable) :
                default(CciDirectoryEntry);
        }

        public CciDirectoryEntry GetImportTableDirectoryEntry(int rva)
        {
            // TODO: constants
            return RequiresStartupStub ?
                new CciDirectoryEntry(rva + ComputeOffsetToImportTable(), (Is32Bit ? 66 : 70) + 13) :
                default(CciDirectoryEntry);
        }

        public CciDirectoryEntry GetCorHeaderDirectoryEntry(int rva)
        {
            return new CciDirectoryEntry(rva + SizeOfImportAddressTable, CorHeaderSize);
        }

        #region Serialization

        public void WriteTextSection(
            Stream peStream,
            int relativeVirtualAddess,
            int pointerToRawData,
            int entryPointToken,
            CorFlags corFlags,
            ulong baseAddress,
            int fileAlignment,
            BlobBuilder metadataWriter,
            BlobBuilder ilWriter,
            BlobBuilder mappedFieldDataWriter,
            BlobBuilder managedResourceWriter,
            ContentId nativePdbContentId,
            ContentId portablePdbContentId,
            out long metadataPosition)
        {
            // TODO: zero out all bytes:
            peStream.Position = pointerToRawData;

            // TODO: avoid multiple recalculation
            int importTableRva = GetImportTableDirectoryEntry(relativeVirtualAddess).RelativeVirtualAddress;
            int importAddressTableRva = GetImportAddressTableDirectoryEntry(relativeVirtualAddess).RelativeVirtualAddress;

            if (RequiresStartupStub)
            {
                WriteImportAddressTable(peStream, importTableRva);
            }

            var corHeader = CreateCorHeader(relativeVirtualAddess, entryPointToken, corFlags);
            WriteCorHeader(peStream, corHeader);

            // IL:
            ilWriter.Align(4);
            ilWriter.WriteContentTo(peStream);

            // metadata:
            metadataPosition = peStream.Position;
            Debug.Assert(metadataWriter.Count % 4 == 0);
            metadataWriter.WriteContentTo(peStream);

            // managed resources:
            Debug.Assert(managedResourceWriter.Count % 4 == 0);
            managedResourceWriter.WriteContentTo(peStream);

            // strong name signature:
            WriteSpaceForHash(peStream, StrongNameSignatureSize);

            if (EmitPdb || IsDeterministic)
            {
                WriteDebugTable(peStream, relativeVirtualAddess, pointerToRawData, nativePdbContentId, portablePdbContentId);
            }

            if (RequiresStartupStub)
            {
                WriteImportTable(peStream, importTableRva, importAddressTableRva);
                WriteNameTable(peStream);
                WriteRuntimeStartupStub(peStream, importAddressTableRva, baseAddress);
            }

            // mapped field data:            
            mappedFieldDataWriter.WriteContentTo(peStream);

            // TODO: zero out all bytes:
            int sizeOfTextSection = ComputeSizeOfTextSection();
            int alignedPosition = pointerToRawData + BitArithmeticUtilities.Align(sizeOfTextSection, fileAlignment);
            if (peStream.Position != alignedPosition)
            {
                peStream.Position = alignedPosition - 1;
                peStream.WriteByte(0);
            }
        }

        private CciCorHeader CreateCorHeader(int textSectionRva, int entryPointToken, CorFlags corFlags)
        {
            int metadataRva = textSectionRva + ComputeOffsetToMetadata(ILStreamSize);
            int resourcesRva = metadataRva + MetadataSize;
            int signatureRva = resourcesRva + ResourceDataSize;

            return new CciCorHeader(
                entryPointTokenOrRelativeVirtualAddress: entryPointToken,
                flags: corFlags,
                metadataDirectory: new CciDirectoryEntry(metadataRva, MetadataSize),
                resourcesDirectory: new CciDirectoryEntry(resourcesRva, ResourceDataSize),
                strongNameSignatureDirectory: new CciDirectoryEntry(signatureRva, StrongNameSignatureSize));
        }

        private void WriteImportAddressTable(Stream peStream, int importTableRva)
        {
            var writer = new BlobBuilder(SizeOfImportAddressTable);
            int ilRVA = importTableRva + 40;
            int hintRva = ilRVA + (Is32Bit ? 12 : 16);

            // Import Address Table
            if (Is32Bit)
            {
                writer.WriteUInt32((uint)hintRva); // 4
                writer.WriteUInt32(0); // 8
            }
            else
            {
                writer.WriteUInt64((uint)hintRva); // 8
                writer.WriteUInt64(0); // 16
            }

            Debug.Assert(writer.Count == SizeOfImportAddressTable);
            writer.WriteContentTo(peStream);
        }

        private void WriteImportTable(Stream peStream, int importTableRva, int importAddressTableRva)
        {
            var writer = new BlobBuilder(SizeOfImportTable);
            int ilRVA = importTableRva + 40;
            int hintRva = ilRVA + (Is32Bit ? 12 : 16);
            int nameRva = hintRva + 12 + 2;

            // Import table
            writer.WriteUInt32((uint)ilRVA); // 4
            writer.WriteUInt32(0); // 8
            writer.WriteUInt32(0); // 12
            writer.WriteUInt32((uint)nameRva); // 16
            writer.WriteUInt32((uint)importAddressTableRva); // 20
            writer.WriteBytes(0, 20); // 40

            // Import Lookup table
            if (Is32Bit)
            {
                writer.WriteUInt32((uint)hintRva); // 44
                writer.WriteUInt32(0); // 48
                writer.WriteUInt32(0); // 52
            }
            else
            {
                writer.WriteUInt64((uint)hintRva); // 48
                writer.WriteUInt64(0); // 56
            }

            // Hint table
            writer.WriteUInt16(0); // Hint 54|58

            foreach (char ch in CorEntryPointName)
            {
                writer.WriteByte((byte)ch); // 65|69
            }

            writer.WriteByte(0); // 66|70
            Debug.Assert(writer.Count == SizeOfImportTable);

            writer.WriteContentTo(peStream);
        }

        private static void WriteNameTable(Stream peStream)
        {
            var writer = new BlobBuilder(SizeOfNameTable);
            foreach (char ch in CorEntryPointDll)
            {
                writer.WriteByte((byte)ch);
            }

            writer.WriteByte(0);
            writer.WriteUInt16(0);
            Debug.Assert(writer.Count == SizeOfNameTable);

            writer.WriteContentTo(peStream);
        }

        private static void WriteCorHeader(Stream peStream, CciCorHeader corHeader)
        {
            var writer = new BlobBuilder(CorHeaderSize);
            writer.WriteUInt32(CorHeaderSize);
            writer.WriteUInt16(corHeader.MajorRuntimeVersion);
            writer.WriteUInt16(corHeader.MinorRuntimeVersion);
            writer.WriteUInt32((uint)corHeader.MetadataDirectory.RelativeVirtualAddress);
            writer.WriteUInt32((uint)corHeader.MetadataDirectory.Size);
            writer.WriteUInt32((uint)corHeader.Flags);
            writer.WriteUInt32((uint)corHeader.EntryPointTokenOrRelativeVirtualAddress);
            writer.WriteUInt32((uint)(corHeader.ResourcesDirectory.Size == 0 ? 0 : corHeader.ResourcesDirectory.RelativeVirtualAddress)); // 28
            writer.WriteUInt32((uint)corHeader.ResourcesDirectory.Size);
            writer.WriteUInt32((uint)(corHeader.StrongNameSignatureDirectory.Size == 0 ? 0 : corHeader.StrongNameSignatureDirectory.RelativeVirtualAddress)); // 36
            writer.WriteUInt32((uint)corHeader.StrongNameSignatureDirectory.Size);
            writer.WriteUInt32((uint)corHeader.CodeManagerTableDirectory.RelativeVirtualAddress);
            writer.WriteUInt32((uint)corHeader.CodeManagerTableDirectory.Size);
            writer.WriteUInt32((uint)corHeader.VtableFixupsDirectory.RelativeVirtualAddress);
            writer.WriteUInt32((uint)corHeader.VtableFixupsDirectory.Size);
            writer.WriteUInt32((uint)corHeader.ExportAddressTableJumpsDirectory.RelativeVirtualAddress);
            writer.WriteUInt32((uint)corHeader.ExportAddressTableJumpsDirectory.Size);
            writer.WriteUInt64(0);
            Debug.Assert(writer.Count == CorHeaderSize);
            Debug.Assert(writer.Count % 4 == 0);

            writer.WriteContentTo(peStream);
        }

        private static void WriteSpaceForHash(Stream peStream, int strongNameSignatureSize)
        {
            while (strongNameSignatureSize > 0)
            {
                peStream.WriteByte(0);
                strongNameSignatureSize--;
            }
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
        private void WriteDebugTable(Stream peStream, int textRva, int textPointer, ContentId nativePdbContentId, ContentId portablePdbContentId)
        {
            int tableSize = ImageDebugDirectoryBaseSize;
            Debug.Assert(tableSize != 0);
            Debug.Assert(nativePdbContentId.IsDefault || portablePdbContentId.IsDefault);
            Debug.Assert(!EmitPdb || (nativePdbContentId.IsDefault ^ portablePdbContentId.IsDefault));

            var writer = Microsoft.Cci.PooledBlobBuilder.GetInstance();

            int dataSize = ComputeSizeOfDebugDirectoryData();
            if (EmitPdb)
            {
                const int IMAGE_DEBUG_TYPE_CODEVIEW = 2; // from PE spec
                uint dataOffset = (uint)(ComputeOffsetToDebugTable() + tableSize);
                WriteDebugTableEntry(writer,
                    stamp: nativePdbContentId.Stamp ?? portablePdbContentId.Stamp,
                    version: portablePdbContentId.IsDefault ? (uint)0 : ('P' << 24 | 'M' << 16 | 0x01 << 8 | 0x00),
                    debugType: IMAGE_DEBUG_TYPE_CODEVIEW,
                    sizeOfData: (uint)dataSize,
                    addressOfRawData: (uint)textRva + dataOffset, // RVA of the data
                    pointerToRawData: (uint)textPointer + dataOffset); // position of the data in the PE stream
            }

            if (IsDeterministic)
            {
                const int IMAGE_DEBUG_TYPE_NO_TIMESTAMP = 16; // from PE spec
                WriteDebugTableEntry(writer,
                    stamp: zeroStamp,
                    version: 0,
                    debugType: IMAGE_DEBUG_TYPE_NO_TIMESTAMP,
                    sizeOfData: 0,
                    addressOfRawData: 0,
                    pointerToRawData: 0);
            }

            // We should now have written all and precisely the data we said we'd write for the table entries.
            Debug.Assert(writer.Count == tableSize);

            // ====================
            // The following is additional data beyond the debug directory at the offset `dataOffset`
            // pointed to by the ImageDebugTypeCodeView entry.

            if (EmitPdb)
            {
                writer.WriteByte((byte)'R');
                writer.WriteByte((byte)'S');
                writer.WriteByte((byte)'D');
                writer.WriteByte((byte)'S');

                // PDB id:
                writer.WriteBytes(nativePdbContentId.Guid ?? portablePdbContentId.Guid);

                // age
                writer.WriteUInt32(Microsoft.Cci.PdbWriter.Age);

                // UTF-8 encoded zero-terminated path to PDB
                int pathStart = writer.Position;
                writer.WriteUTF8(PdbPathOpt, allowUnpairedSurrogates: true);
                writer.WriteByte(0);

                // padding:
                writer.WriteBytes(0, Math.Max(0, MinPdbPath - (writer.Position - pathStart)));
            }

            // We should now have written all and precisely the data we said we'd write for the table and its data.
            Debug.Assert(writer.Count == tableSize + dataSize);

            writer.WriteContentTo(peStream);
            writer.Free();
        }

        private void WriteRuntimeStartupStub(Stream peStream, int importAddressTableRva, ulong baseAddress)
        {
            var writer = new BlobBuilder(16);
            // entry point code, consisting of a jump indirect to _CorXXXMain
            if (Is32Bit)
            {
                //emit 0's (nops) to pad the entry point code so that the target address is aligned on a 4 byte boundary.
                for (uint i = 0, n = (uint)(BitArithmeticUtilities.Align((uint)peStream.Position, 4) - peStream.Position); i < n; i++)
                {
                    writer.WriteByte(0);
                }

                writer.WriteUInt16(0);
                writer.WriteByte(0xff);
                writer.WriteByte(0x25); //4
                writer.WriteUInt32((uint)importAddressTableRva + (uint)baseAddress); //8
            }
            else
            {
                //emit 0's (nops) to pad the entry point code so that the target address is aligned on a 8 byte boundary.
                for (uint i = 0, n = (uint)(BitArithmeticUtilities.Align((uint)peStream.Position, 8) - peStream.Position); i < n; i++)
                {
                    writer.WriteByte(0);
                }

                writer.WriteUInt32(0);
                writer.WriteUInt16(0);
                writer.WriteByte(0xff);
                writer.WriteByte(0x25); //8
                writer.WriteUInt64((ulong)importAddressTableRva + baseAddress); //16
            }

            writer.WriteContentTo(peStream);
        }

        #endregion
    }
}
