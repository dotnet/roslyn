// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection.PortableExecutable;

namespace Microsoft.Cci
{
    internal sealed class NtHeader
    {
        // standard fields
        internal PEMagic Magic;
        internal byte MajorLinkerVersion;
        internal byte MinorLinkerVersion;
        internal int SizeOfCode;
        internal int SizeOfInitializedData;
        internal int SizeOfUninitializedData;
        internal int AddressOfEntryPoint;
        internal int BaseOfCode; // this.sectionHeaders[0].virtualAddress
        internal int BaseOfData;

        // Windows

        internal ulong ImageBase;
        internal int SectionAlignment = 0x2000;
        internal int FileAlignment;

        internal ushort MajorOperatingSystemVersion = 4;
        internal ushort MinorOperatingSystemVersion = 0;
        internal ushort MajorImageVersion = 0;
        internal ushort MinorImageVersion = 0;
        internal ushort MajorSubsystemVersion;
        internal ushort MinorSubsystemVersion;

        internal int SizeOfImage;
        internal int SizeOfHeaders;
        internal uint Checksum = 0;

        internal Subsystem Subsystem;
        internal DllCharacteristics DllCharacteristics;

        internal ulong SizeOfStackReserve;
        internal ulong SizeOfStackCommit;
        internal ulong SizeOfHeapReserve;
        internal ulong SizeOfHeapCommit;

        internal DirectoryEntry ExportTable;
        internal DirectoryEntry ImportTable;
        internal DirectoryEntry ResourceTable;
        internal DirectoryEntry ExceptionTable;
        internal DirectoryEntry CertificateTable;
        internal DirectoryEntry BaseRelocationTable;
        internal DirectoryEntry DebugTable;
        internal DirectoryEntry CopyrightTable;
        internal DirectoryEntry GlobalPointerTable;
        internal DirectoryEntry ThreadLocalStorageTable;
        internal DirectoryEntry LoadConfigTable;
        internal DirectoryEntry BoundImportTable;
        internal DirectoryEntry ImportAddressTable;
        internal DirectoryEntry DelayImportTable;
        internal DirectoryEntry CliHeaderTable;
    }
}
