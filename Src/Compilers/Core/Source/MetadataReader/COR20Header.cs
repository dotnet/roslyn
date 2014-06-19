using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal struct COR20Header
    {
        internal int CountBytes;
        internal ushort MajorRuntimeVersion;
        internal ushort MinorRuntimeVersion;
        internal DirectoryEntry MetaDataDirectory;
        internal COR20Flags COR20Flags;
        internal uint EntryPointTokenOrRVA;
        internal DirectoryEntry ResourcesDirectory;
        internal DirectoryEntry StrongNameSignatureDirectory;
        internal DirectoryEntry CodeManagerTableDirectory;
        internal DirectoryEntry VtableFixupsDirectory;
        internal DirectoryEntry ExportAddressTableJumpsDirectory;
        internal DirectoryEntry ManagedNativeHeaderDirectory;
    }
}