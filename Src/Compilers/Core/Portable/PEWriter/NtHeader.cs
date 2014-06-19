// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Cci = Microsoft.Cci;

namespace Microsoft.Cci
{
    internal class NtHeader
    {
        internal ushort NumberOfSections;
        internal uint TimeDateStamp;
        internal uint PointerToSymbolTable;
        internal uint SizeOfCode;
        internal uint SizeOfInitializedData;
        internal uint SizeOfUninitializedData;
        internal uint AddressOfEntryPoint;
        internal uint BaseOfCode; // this.sectionHeaders[0].virtualAddress
        internal uint BaseOfData;
        internal uint SizeOfImage;
        internal uint SizeOfHeaders;
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
        internal DirectoryEntry Reserved;
    }
}