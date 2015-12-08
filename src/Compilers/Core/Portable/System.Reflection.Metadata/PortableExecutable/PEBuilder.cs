// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection.Metadata.Ecma335;
using Roslyn.Utilities;
using CciDirectoryEntry = Microsoft.Cci.DirectoryEntry;

namespace System.Reflection.PortableExecutable
{
    internal sealed class PEBuilder
    {
        // COFF:
        public bool Is32Bit { get; }
        public Machine Machine { get; set; }
        public int TimeDateStamp { get; set; }
        public Characteristics ImageCharacteristics { get; set; }
        
        // PE:
        public byte MajorLinkerVersion { get; set; }
        public byte MinorLinkerVersion { get; set; }

        public int AddressOfEntryPoint { get; set; }

        public ulong ImageBase { get; set; }
        public int SectionAlignment { get; }
        public int FileAlignment { get; }

        public ushort MajorOperatingSystemVersion { get; set; } = 4;
        public ushort MinorOperatingSystemVersion { get; set; }

        public ushort MajorImageVersion { get; set; }
        public ushort MinorImageVersion { get; set; }

        public ushort MajorSubsystemVersion { get; set; }
        public ushort MinorSubsystemVersion { get; set; }

        public Subsystem Subsystem { get; set; }
        public DllCharacteristics DllCharacteristics { get; set; }

        public ulong SizeOfStackReserve { get; set; }
        public ulong SizeOfStackCommit { get; set; }
        public ulong SizeOfHeapReserve { get; set; }
        public ulong SizeOfHeapCommit { get; set; }

        public CciDirectoryEntry ExportTable { get; set; }
        public CciDirectoryEntry ImportTable { get; set; }
        public CciDirectoryEntry ResourceTable { get; set; }
        public CciDirectoryEntry ExceptionTable { get; set; }
        public CciDirectoryEntry CertificateTable { get; set; }
        public CciDirectoryEntry BaseRelocationTable { get; set; }
        public CciDirectoryEntry DebugTable { get; set; }
        public CciDirectoryEntry CopyrightTable { get; set; }
        public CciDirectoryEntry GlobalPointerTable { get; set; }
        public CciDirectoryEntry ThreadLocalStorageTable { get; set; }
        public CciDirectoryEntry LoadConfigTable { get; set; }
        public CciDirectoryEntry BoundImportTable { get; set; }
        public CciDirectoryEntry ImportAddressTable { get; set; }
        public CciDirectoryEntry DelayImportTable { get; set; }
        public CciDirectoryEntry CorHeaderTable { get; set; }

        private readonly Section[] _sections;
        private int _currentSection;
        public PESectionLocation NextSectionLocation { get; private set; }

        private struct Section
        {
            public readonly string Name;
            public readonly SectionCharacteristics Characteristics;
            public readonly BlobBuilder Builder;
            public readonly int VirtualSize; // unaligned
            public readonly int RelativeVirtualAddress;
            public readonly int SizeOfRawData; // aligned to FileAlignment
            public readonly int PointerToRawData; // aligned to FileAlignment

            public Section(
                string name,
                SectionCharacteristics characteristics,
                BlobBuilder builder,
                int virtualSize,
                int relativeVirtualAddress,
                int sizeOfRawData,
                int pointerToRawData)
            {
                Name = name;
                Characteristics = characteristics;
                Builder = builder;
                VirtualSize = virtualSize;
                RelativeVirtualAddress = relativeVirtualAddress;
                SizeOfRawData = sizeOfRawData;
                PointerToRawData = pointerToRawData;
            }

            public bool IsDefault => Name == null;
        }

        public PEBuilder(
            int sectionCount,
            int sectionAlignment,
            int fileAlignment,
            bool is32Bit)
        {
            SectionAlignment = sectionAlignment;
            FileAlignment = fileAlignment;
            Is32Bit = is32Bit;
            
            _sections = new Section[sectionCount];
            int sizeOfPeHeaders = ComputeSizeOfPeHeaders(sectionCount, is32Bit);

            NextSectionLocation = new PESectionLocation(
                BitArithmeticUtilities.Align(sizeOfPeHeaders, sectionAlignment),
                BitArithmeticUtilities.Align(sizeOfPeHeaders, fileAlignment));
        }

        public PESectionLocation AddSection(string name, SectionCharacteristics characteristics, BlobBuilder builder)
        {
            if (_currentSection == _sections.Length)
            {
                // TODO: message
                throw new InvalidOperationException();
            }

            var section = new Section(
                name,
                characteristics,
                builder,
                pointerToRawData: NextSectionLocation.PointerToRawData, 
                relativeVirtualAddress: NextSectionLocation.RelativeVirtualAddress,
                sizeOfRawData: BitArithmeticUtilities.Align(builder.Count, FileAlignment),
                virtualSize: builder.Count);

            _sections[_currentSection] = section;
            _currentSection++;

            NextSectionLocation = new PESectionLocation(
                BitArithmeticUtilities.Align(section.RelativeVirtualAddress + section.VirtualSize, SectionAlignment),
                section.PointerToRawData + section.SizeOfRawData);

            return new PESectionLocation(section.RelativeVirtualAddress, section.PointerToRawData);
        }

        private int ComputeSizeOfPeHeaders()
        {
            return ComputeSizeOfPeHeaders(_sections.Length, Is32Bit);
        }

        private static int ComputeSizeOfPeHeaders(int sectionCount, bool is32Bit)
        {
            // TODO: constants
            int sizeOfPeHeaders = 128 + 4 + 20 + 224 + 40 * sectionCount;
            if (!is32Bit)
            {
                sizeOfPeHeaders += 16;
            }

            return sizeOfPeHeaders;
        }

        public void Serialize(BlobBuilder builder, out long timestampPosition)
        {
            WritePESignature(builder);
            WriteCoffHeader(builder, out timestampPosition);
            WritePEHeader(builder);
            WriteSectionHeaders(builder);
            builder.Align(FileAlignment);

            foreach (var section in _sections)
            {
                if (section.Builder.Count != section.VirtualSize)
                {
                    // TODO: message - builder changed count
                    throw new InvalidOperationException();
                }

                builder.LinkSuffix(section.Builder);
                builder.Align(FileAlignment);
            }
        }

        private void WritePESignature(BlobBuilder builder)
        {
            // MS-DOS stub (128 bytes)
            builder.WriteBytes(s_dosHeader);

            // PE Signature "PE\0\0" 
            builder.WriteUInt32(0x00004550);
        }

        private static readonly byte[] s_dosHeader = new byte[]
        {
            0x4d, 0x5a, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0xff, 0xff, 0x00, 0x00,
            0xb8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00,
            0x0e, 0x1f, 0xba, 0x0e, 0x00, 0xb4, 0x09, 0xcd,
            0x21, 0xb8, 0x01, 0x4c, 0xcd, 0x21, 0x54, 0x68,
            0x69, 0x73, 0x20, 0x70, 0x72, 0x6f, 0x67, 0x72,
            0x61, 0x6d, 0x20, 0x63, 0x61, 0x6e, 0x6e, 0x6f,
            0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6e,
            0x20, 0x69, 0x6e, 0x20, 0x44, 0x4f, 0x53, 0x20,
            0x6d, 0x6f, 0x64, 0x65, 0x2e, 0x0d, 0x0d, 0x0a,
            0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private void WriteCoffHeader(BlobBuilder builder, out long timestampPosition)
        {
            // Machine
            builder.WriteUInt16((ushort)(Machine == 0 ? Machine.I386 : Machine));

            // NumberOfSections
            builder.WriteUInt16((ushort)_sections.Length);

            // TimeDateStamp:
            timestampPosition = builder.Position;
            builder.WriteUInt32((uint)TimeDateStamp);

            // PointerToSymbolTable (TODO: not supported):
            // The file pointer to the COFF symbol table, or zero if no COFF symbol table is present. 
            // This value should be zero for a PE image.
            builder.WriteUInt32(0);

            // NumberOfSymbols (TODO: not supported):
            // The number of entries in the symbol table. This data can be used to locate the string table, 
            // which immediately follows the symbol table. This value should be zero for a PE image.
            builder.WriteUInt32(0);

            // SizeOfOptionalHeader:
            // The size of the optional header, which is required for executable files but not for object files. 
            // This value should be zero for an object file (TODO).
            builder.WriteUInt16((ushort)(Is32Bit ? 224 : 240));

            // Characteristics
            builder.WriteUInt16((ushort)ImageCharacteristics);
        }

        private void WritePEHeader(BlobBuilder builder)
        {
            builder.WriteUInt16((ushort)(Is32Bit ? PEMagic.PE32 : PEMagic.PE32Plus));
            builder.WriteByte(MajorLinkerVersion);
            builder.WriteByte(MinorLinkerVersion);

            // SizeOfCode:
            builder.WriteUInt32((uint)SumRawDataSizes(SectionCharacteristics.ContainsCode));

            // SizeOfInitializedData:
            builder.WriteUInt32((uint)SumRawDataSizes(SectionCharacteristics.ContainsInitializedData));

            // SizeOfUninitializedData:
            builder.WriteUInt32((uint)SumRawDataSizes(SectionCharacteristics.ContainsUninitializedData));

            // AddressOfEntryPoint:
            builder.WriteUInt32((uint)AddressOfEntryPoint);

            // BaseOfCode:
            int codeSectionIndex = IndexOfSection(SectionCharacteristics.ContainsCode);
            builder.WriteUInt32((uint)(codeSectionIndex != -1 ? _sections[codeSectionIndex].RelativeVirtualAddress : 0));

            if (Is32Bit)
            {
                // BaseOfData:
                int dataSectionIndex = IndexOfSection(SectionCharacteristics.ContainsInitializedData);
                builder.WriteUInt32((uint)(dataSectionIndex != -1 ? _sections[dataSectionIndex].RelativeVirtualAddress : 0));

                builder.WriteUInt32((uint)ImageBase);
            }
            else
            {
                builder.WriteUInt64(ImageBase);
            }

            // NT additional fields:
            builder.WriteUInt32((uint)SectionAlignment);
            builder.WriteUInt32((uint)FileAlignment);
            builder.WriteUInt16(MajorOperatingSystemVersion);
            builder.WriteUInt16(MinorOperatingSystemVersion);
            builder.WriteUInt16(MajorImageVersion);
            builder.WriteUInt16(MinorImageVersion);
            builder.WriteUInt16(MajorSubsystemVersion);
            builder.WriteUInt16(MinorSubsystemVersion);

            // Win32VersionValue (reserved, should be 0)
            builder.WriteUInt32(0);

            // SizeOfImage:
            var lastSection = _sections[_sections.Length - 1];
            builder.WriteUInt32((uint)BitArithmeticUtilities.Align(lastSection.RelativeVirtualAddress + lastSection.VirtualSize, SectionAlignment));

            // SizeOfHeaders:
            builder.WriteUInt32((uint)BitArithmeticUtilities.Align(ComputeSizeOfPeHeaders(), FileAlignment));

            // Checksum (TODO: not supported):
            builder.WriteUInt32(0);

            builder.WriteUInt16((ushort)Subsystem);
            builder.WriteUInt16((ushort)DllCharacteristics);

            if (Is32Bit)
            {
                builder.WriteUInt32((uint)SizeOfStackReserve);
                builder.WriteUInt32((uint)SizeOfStackCommit);
                builder.WriteUInt32((uint)SizeOfHeapReserve);
                builder.WriteUInt32((uint)SizeOfHeapCommit);
            }
            else
            {
                builder.WriteUInt64(SizeOfStackReserve);
                builder.WriteUInt64(SizeOfStackCommit);
                builder.WriteUInt64(SizeOfHeapReserve);
                builder.WriteUInt64(SizeOfHeapCommit);
            }

            // LoaderFlags
            builder.WriteUInt32(0);

            // The number of data-directory entries in the remainder of the header.
            builder.WriteUInt32(16);

            // directory entries:
            builder.WriteUInt32((uint)ExportTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)ExportTable.Size);
            builder.WriteUInt32((uint)ImportTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)ImportTable.Size);
            builder.WriteUInt32((uint)ResourceTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)ResourceTable.Size);
            builder.WriteUInt32((uint)ExceptionTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)ExceptionTable.Size);
            builder.WriteUInt32((uint)CertificateTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)CertificateTable.Size);
            builder.WriteUInt32((uint)BaseRelocationTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)BaseRelocationTable.Size);
            builder.WriteUInt32((uint)DebugTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)DebugTable.Size);
            builder.WriteUInt32((uint)CopyrightTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)CopyrightTable.Size);
            builder.WriteUInt32((uint)GlobalPointerTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)GlobalPointerTable.Size);
            builder.WriteUInt32((uint)ThreadLocalStorageTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)ThreadLocalStorageTable.Size);
            builder.WriteUInt32((uint)LoadConfigTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)LoadConfigTable.Size);
            builder.WriteUInt32((uint)BoundImportTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)BoundImportTable.Size);
            builder.WriteUInt32((uint)ImportAddressTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)ImportAddressTable.Size);
            builder.WriteUInt32((uint)DelayImportTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)DelayImportTable.Size);
            builder.WriteUInt32((uint)CorHeaderTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)CorHeaderTable.Size);

            // Reserved, should be 0
            builder.WriteUInt64(0);
        }

        private void WriteSectionHeaders(BlobBuilder builder)
        {
            foreach (var section in _sections)
            {
                WriteSectionHeader(builder, section);
            }
        }

        private static void WriteSectionHeader(BlobBuilder builder, Section section)
        {
            if (section.VirtualSize == 0)
            {
                return;
            }

            for (int j = 0, m = section.Name.Length; j < 8; j++)
            {
                if (j < m)
                {
                    builder.WriteByte((byte)section.Name[j]);
                }
                else
                {
                    builder.WriteByte(0);
                }
            }

            builder.WriteUInt32((uint)section.VirtualSize);
            builder.WriteUInt32((uint)section.RelativeVirtualAddress);
            builder.WriteUInt32((uint)section.SizeOfRawData);
            builder.WriteUInt32((uint)section.PointerToRawData);

            // PointerToRelocations (TODO: not supported):
            builder.WriteUInt32(0);

            // PointerToLinenumbers (TODO: not supported):
            builder.WriteUInt32(0);

            // NumberOfRelocations (TODO: not supported):
            builder.WriteUInt16(0);

            // NumberOfLinenumbers (TODO: not supported):
            builder.WriteUInt16(0);

            builder.WriteUInt32((uint)section.Characteristics);
        }

        private int IndexOfSection(SectionCharacteristics characteristics)
        {
            for (int i = 0; i < _sections.Length; i++)
            {
                if ((_sections[i].Characteristics & characteristics) == characteristics)
                {
                    return i;
                }
            }

            return -1;
        }

        private int SumRawDataSizes(SectionCharacteristics characteristics)
        {
            int result = 0;
            foreach (var section in _sections)
            {
                if ((section.Characteristics & characteristics) == characteristics)
                {
                    result += section.SizeOfRawData;
                }
            }

            return result;
        }
    }
}
