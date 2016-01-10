// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using CciDirectoryEntry = Microsoft.Cci.DirectoryEntry;

namespace System.Reflection.PortableExecutable
{
    internal sealed class PEDirectoriesBuilder
    {
        public int AddressOfEntryPoint { get; set; }

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
    }
}
