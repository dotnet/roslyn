using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class COR20Constants
    {
        internal const int SizeOfCOR20Header = 72;
        internal const uint COR20MetadataSignature = 0x424A5342;
        internal const int MinimumSizeofMetadataHeader = 16;
        internal const int SizeofStorageHeader = 4;
        internal const int MinimumSizeofStreamHeader = 8;
        internal const string StringStreamName = "#Strings";
        internal const string BlobStreamName = "#Blob";
        internal const string GUIDStreamName = "#GUID";
        internal const string UserStringStreamName = "#US";
        internal const string CompressedMetadataTableStreamName = "#~";
        internal const string UncompressedMetadataTableStreamName = "#-";
        internal const int LargeStreamHeapSize = 0x0001000;
    }
}