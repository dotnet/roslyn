// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection.PortableExecutable;

#if SRM
namespace System.Reflection.PortableExecutable
#else
namespace Roslyn.Reflection.PortableExecutable
#endif
{
#if SRM
    public
#endif
    sealed class PEDirectoriesBuilder
    {
        public int AddressOfEntryPoint { get; set; }

        public DirectoryEntry ExportTable { get; set; }
        public DirectoryEntry ImportTable { get; set; }
        public DirectoryEntry ResourceTable { get; set; }
        public DirectoryEntry ExceptionTable { get; set; }
        public DirectoryEntry CertificateTable { get; set; }
        public DirectoryEntry BaseRelocationTable { get; set; }
        public DirectoryEntry DebugTable { get; set; }
        public DirectoryEntry CopyrightTable { get; set; }
        public DirectoryEntry GlobalPointerTable { get; set; }
        public DirectoryEntry ThreadLocalStorageTable { get; set; }
        public DirectoryEntry LoadConfigTable { get; set; }
        public DirectoryEntry BoundImportTable { get; set; }
        public DirectoryEntry ImportAddressTable { get; set; }
        public DirectoryEntry DelayImportTable { get; set; }
        public DirectoryEntry CorHeaderTable { get; set; }
    }
}
