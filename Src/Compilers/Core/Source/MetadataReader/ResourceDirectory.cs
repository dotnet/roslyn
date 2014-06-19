using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal struct ResourceDirectory
    {
        internal uint Charecteristics;
        internal uint TimeDateStamp;
        internal short MajorVersion;
        internal short MinorVersion;
        internal short NumberOfNamedEntries;
        internal short NumberOfIdEntries;
    }
}