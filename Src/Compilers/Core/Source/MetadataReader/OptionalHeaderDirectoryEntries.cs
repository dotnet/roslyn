using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal struct OptionalHeaderDirectoryEntries
    {
        internal DirectoryEntry ExportTableDirectory;
        internal DirectoryEntry ImportTableDirectory;
        internal DirectoryEntry ResourceTableDirectory;
        internal DirectoryEntry ExceptionTableDirectory;
        internal DirectoryEntry CertificateTableDirectory;
        internal DirectoryEntry BaseRelocationTableDirectory;
        internal DirectoryEntry DebugTableDirectory;
        internal DirectoryEntry CopyrightTableDirectory;
        internal DirectoryEntry GlobalPointerTableDirectory;
        internal DirectoryEntry ThreadLocalStorageTableDirectory;
        internal DirectoryEntry LoadConfigTableDirectory;
        internal DirectoryEntry BoundImportTableDirectory;
        internal DirectoryEntry ImportAddressTableDirectory;
        internal DirectoryEntry DelayImportTableDirectory;
        internal DirectoryEntry COR20HeaderTableDirectory;
        internal DirectoryEntry ReservedDirectory;
    }
}