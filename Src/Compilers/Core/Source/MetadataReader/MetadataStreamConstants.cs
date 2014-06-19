using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class MetadataStreamConstants
    {
        internal const int SizeOfMetadataTableHeader = 24;
        internal const uint LargeTableRowCount = 0x00010000;
    }
}