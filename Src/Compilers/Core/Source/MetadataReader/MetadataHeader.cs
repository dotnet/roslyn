using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal struct MetadataHeader
    {
        internal uint Signature;
        internal ushort MajorVersion;
        internal ushort MinorVersion;
        internal uint ExtraData;
        internal int VersionStringSize;
        internal string VersionString;
    }
}