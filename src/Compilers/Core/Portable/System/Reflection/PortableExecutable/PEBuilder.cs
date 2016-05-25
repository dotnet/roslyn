// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.PortableExecutable;

#if SRM
using System.Reflection.Internal;
using BitArithmeticUtilities = System.Reflection.Internal.BitArithmetic;
#else
using Roslyn.Utilities;
#endif

#if SRM
namespace System.Reflection.PortableExecutable
#else
namespace Roslyn.Reflection.PortableExecutable
#endif
{
#if SRM
    public
#endif
    sealed class PEBuilder
    {
        // COFF:
        public Machine Machine { get; }
        public Characteristics ImageCharacteristics { get; set; }
        public bool IsDeterministic { get; }
        
        // PE:
        public byte MajorLinkerVersion { get; }
        public byte MinorLinkerVersion { get; }

        public ulong ImageBase { get; }
        public int SectionAlignment { get; }
        public int FileAlignment { get; }

        public ushort MajorOperatingSystemVersion { get; }
        public ushort MinorOperatingSystemVersion { get; }

        public ushort MajorImageVersion { get; }
        public ushort MinorImageVersion { get; }

        public ushort MajorSubsystemVersion { get; }
        public ushort MinorSubsystemVersion { get; }

        public Subsystem Subsystem { get; }
        public DllCharacteristics DllCharacteristics { get; }

        public ulong SizeOfStackReserve { get; }
        public ulong SizeOfStackCommit { get; }
        public ulong SizeOfHeapReserve { get; }
        public ulong SizeOfHeapCommit { get; }

        public Func<BlobBuilder, ContentId> IdProvider { get; }

        private readonly List<Section> _sections;

        private struct Section
        {
            public readonly string Name;
            public readonly SectionCharacteristics Characteristics;
            public readonly Func<PESectionLocation, BlobBuilder> Builder;

            public Section(string name, SectionCharacteristics characteristics, Func<PESectionLocation, BlobBuilder> builder)
            {
                Name = name;
                Characteristics = characteristics;
                Builder = builder;
            }
        }

        private struct SerializedSection
        {
            public readonly BlobBuilder Builder;

            public readonly string Name;
            public readonly SectionCharacteristics Characteristics;
            public readonly int RelativeVirtualAddress;
            public readonly int SizeOfRawData;
            public readonly int PointerToRawData;

            public SerializedSection(BlobBuilder builder, string name, SectionCharacteristics characteristics, int relativeVirtualAddress, int sizeOfRawData, int pointerToRawData)
            {
                Name = name;
                Characteristics = characteristics;
                Builder = builder;
                RelativeVirtualAddress = relativeVirtualAddress;
                SizeOfRawData = sizeOfRawData;
                PointerToRawData = pointerToRawData;
            }

            public int VirtualSize => Builder.Count;
        }

        public PEBuilder(
            Machine machine,
            int sectionAlignment, 
            int fileAlignment, 
            ulong imageBase,
            byte majorLinkerVersion,
            byte minorLinkerVersion,
            ushort majorOperatingSystemVersion,
            ushort minorOperatingSystemVersion,
            ushort majorImageVersion,
            ushort minorImageVersion,
            ushort majorSubsystemVersion,
            ushort minorSubsystemVersion,
            Subsystem subsystem,
            DllCharacteristics dllCharacteristics,
            Characteristics imageCharacteristics,
            ulong sizeOfStackReserve,
            ulong sizeOfStackCommit,
            ulong sizeOfHeapReserve,
            ulong sizeOfHeapCommit,
            Func<BlobBuilder, ContentId> deterministicIdProvider = null)
        {
            Machine = machine;
            SectionAlignment = sectionAlignment;
            FileAlignment = fileAlignment;
            ImageBase = imageBase;
            MajorLinkerVersion = majorLinkerVersion;
            MinorLinkerVersion = minorLinkerVersion;
            MajorOperatingSystemVersion = majorOperatingSystemVersion;
            MinorOperatingSystemVersion = minorOperatingSystemVersion;
            MajorImageVersion = majorImageVersion;
            MinorImageVersion = minorImageVersion;
            MajorSubsystemVersion = majorSubsystemVersion;
            MinorSubsystemVersion = minorSubsystemVersion;
            Subsystem = subsystem;
            DllCharacteristics = dllCharacteristics;
            ImageCharacteristics = imageCharacteristics;
            SizeOfStackReserve = sizeOfStackReserve;
            SizeOfStackCommit = sizeOfStackCommit;
            SizeOfHeapReserve = sizeOfHeapReserve;
            SizeOfHeapCommit = sizeOfHeapCommit;
            IsDeterministic = deterministicIdProvider != null;
            IdProvider = deterministicIdProvider ?? GetCurrentTimeBasedIdProvider();

            _sections = new List<Section>();
        }

        private static Func<BlobBuilder, ContentId> GetCurrentTimeBasedIdProvider()
        {
            // In the PE File Header this is a "Time/Date Stamp" whose description is "Time and date
            // the file was created in seconds since January 1st 1970 00:00:00 or 0"
            // However, when we want to make it deterministic we fill it in (later) with bits from the hash of the full PE file.
            int timestamp = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            return content => new ContentId(Guid.NewGuid(), timestamp);
        }

        private bool Is32Bit => Machine != Machine.Amd64 && Machine != Machine.IA64;

        public void AddSection(string name, SectionCharacteristics characteristics, Func<PESectionLocation, BlobBuilder> builder)
        {
            _sections.Add(new Section(name, characteristics, builder));
        }

        public void Serialize(BlobBuilder builder, PEDirectoriesBuilder headers, out ContentId contentId)
        {
            var serializedSections = SerializeSections();
            Blob stampFixup;

            WritePESignature(builder);
            WriteCoffHeader(builder, serializedSections, out stampFixup);
            WritePEHeader(builder, headers, serializedSections);
            WriteSectionHeaders(builder, serializedSections);
            builder.Align(FileAlignment);

            foreach (var section in serializedSections)
            {
                builder.LinkSuffix(section.Builder);
                builder.Align(FileAlignment);
            }

            contentId = IdProvider(builder);

            // patch timestamp in COFF header:
            var stampWriter = new BlobWriter(stampFixup);
            stampWriter.WriteBytes(contentId.Stamp);
            Debug.Assert(stampWriter.RemainingBytes == 0);
        }

        private ImmutableArray<SerializedSection> SerializeSections()
        {
            var result = ImmutableArray.CreateBuilder<SerializedSection>(_sections.Count);
            int sizeOfPeHeaders = ComputeSizeOfPeHeaders(_sections.Count, Is32Bit);

            var nextRva = BitArithmeticUtilities.Align(sizeOfPeHeaders, SectionAlignment);
            var nextPointer = BitArithmeticUtilities.Align(sizeOfPeHeaders, FileAlignment);

            foreach (var section in _sections)
            {
                var builder = section.Builder(new PESectionLocation(nextRva, nextPointer));

                var serialized = new SerializedSection(
                    builder,
                    section.Name,
                    section.Characteristics,
                    relativeVirtualAddress: nextRva,
                    sizeOfRawData: BitArithmeticUtilities.Align(builder.Count, FileAlignment),
                    pointerToRawData: nextPointer);

                result.Add(serialized);

                nextRva = BitArithmeticUtilities.Align(serialized.RelativeVirtualAddress + serialized.VirtualSize, SectionAlignment);
                nextPointer = serialized.PointerToRawData + serialized.SizeOfRawData;
            }

            return result.MoveToImmutable();
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

        private void WriteCoffHeader(BlobBuilder builder, ImmutableArray<SerializedSection> sections, out Blob stampFixup)
        {
            // Machine
            builder.WriteUInt16((ushort)(Machine == 0 ? Machine.I386 : Machine));

            // NumberOfSections
            builder.WriteUInt16((ushort)sections.Length);

            // TimeDateStamp:
            stampFixup = builder.ReserveBytes(sizeof(uint));

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

        private void WritePEHeader(BlobBuilder builder, PEDirectoriesBuilder headers, ImmutableArray<SerializedSection> sections)
        {
            builder.WriteUInt16((ushort)(Is32Bit ? PEMagic.PE32 : PEMagic.PE32Plus));
            builder.WriteByte(MajorLinkerVersion);
            builder.WriteByte(MinorLinkerVersion);

            // SizeOfCode:
            builder.WriteUInt32((uint)SumRawDataSizes(sections, SectionCharacteristics.ContainsCode));

            // SizeOfInitializedData:
            builder.WriteUInt32((uint)SumRawDataSizes(sections, SectionCharacteristics.ContainsInitializedData));

            // SizeOfUninitializedData:
            builder.WriteUInt32((uint)SumRawDataSizes(sections, SectionCharacteristics.ContainsUninitializedData));

            // AddressOfEntryPoint:
            builder.WriteUInt32((uint)headers.AddressOfEntryPoint);

            // BaseOfCode:
            int codeSectionIndex = IndexOfSection(sections, SectionCharacteristics.ContainsCode);
            builder.WriteUInt32((uint)(codeSectionIndex != -1 ? sections[codeSectionIndex].RelativeVirtualAddress : 0));

            if (Is32Bit)
            {
                // BaseOfData:
                int dataSectionIndex = IndexOfSection(sections, SectionCharacteristics.ContainsInitializedData);
                builder.WriteUInt32((uint)(dataSectionIndex != -1 ? sections[dataSectionIndex].RelativeVirtualAddress : 0));

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
            var lastSection = sections[sections.Length - 1];
            builder.WriteUInt32((uint)BitArithmeticUtilities.Align(lastSection.RelativeVirtualAddress + lastSection.VirtualSize, SectionAlignment));

            // SizeOfHeaders:
            builder.WriteUInt32((uint)BitArithmeticUtilities.Align(ComputeSizeOfPeHeaders(sections.Length, Is32Bit), FileAlignment));

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
            builder.WriteUInt32((uint)headers.ExportTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)headers.ExportTable.Size);
            builder.WriteUInt32((uint)headers.ImportTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)headers.ImportTable.Size);
            builder.WriteUInt32((uint)headers.ResourceTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)headers.ResourceTable.Size);
            builder.WriteUInt32((uint)headers.ExceptionTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)headers.ExceptionTable.Size);
            builder.WriteUInt32((uint)headers.CertificateTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)headers.CertificateTable.Size);
            builder.WriteUInt32((uint)headers.BaseRelocationTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)headers.BaseRelocationTable.Size);
            builder.WriteUInt32((uint)headers.DebugTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)headers.DebugTable.Size);
            builder.WriteUInt32((uint)headers.CopyrightTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)headers.CopyrightTable.Size);
            builder.WriteUInt32((uint)headers.GlobalPointerTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)headers.GlobalPointerTable.Size);
            builder.WriteUInt32((uint)headers.ThreadLocalStorageTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)headers.ThreadLocalStorageTable.Size);
            builder.WriteUInt32((uint)headers.LoadConfigTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)headers.LoadConfigTable.Size);
            builder.WriteUInt32((uint)headers.BoundImportTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)headers.BoundImportTable.Size);
            builder.WriteUInt32((uint)headers.ImportAddressTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)headers.ImportAddressTable.Size);
            builder.WriteUInt32((uint)headers.DelayImportTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)headers.DelayImportTable.Size);
            builder.WriteUInt32((uint)headers.CorHeaderTable.RelativeVirtualAddress);
            builder.WriteUInt32((uint)headers.CorHeaderTable.Size);

            // Reserved, should be 0
            builder.WriteUInt64(0);
        }

        private void WriteSectionHeaders(BlobBuilder builder, ImmutableArray<SerializedSection> serializedSections)
        {
            for (int i = 0; i < serializedSections.Length; i++)
            {
                WriteSectionHeader(builder, _sections[i], serializedSections[i]);
            }
        }

        private static void WriteSectionHeader(BlobBuilder builder, Section section, SerializedSection serializedSection)
        {
            if (serializedSection.VirtualSize == 0)
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

            builder.WriteUInt32((uint)serializedSection.VirtualSize);
            builder.WriteUInt32((uint)serializedSection.RelativeVirtualAddress);
            builder.WriteUInt32((uint)serializedSection.SizeOfRawData);
            builder.WriteUInt32((uint)serializedSection.PointerToRawData);

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

        private static int IndexOfSection(ImmutableArray<SerializedSection> sections, SectionCharacteristics characteristics)
        {
            for (int i = 0; i < sections.Length; i++)
            {
                if ((sections[i].Characteristics & characteristics) == characteristics)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int SumRawDataSizes(ImmutableArray<SerializedSection> sections,SectionCharacteristics characteristics)
        {
            int result = 0;
            for (int i = 0; i < sections.Length; i++)
            {
                if ((sections[i].Characteristics & characteristics) == characteristics)
                {
                    result += sections[i].SizeOfRawData;
                }
            }

            return result;
        }
    }
}
