using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class PEFileConstants
    {
        internal const ushort DosSignature = 0x5A4D;     // MZ
        internal const int PESignatureOffsetLocation = 0x3C;
        internal const uint PESignature = 0x00004550;    // PE00
        internal const int BasicPEHeaderSize = PEFileConstants.PESignatureOffsetLocation;
        internal const int SizeofCOFFFileHeader = 20;
        internal const int SizeofOptionalHeaderStandardFields32 = 28;
        internal const int SizeofOptionalHeaderStandardFields64 = 24;
        internal const int SizeofOptionalHeaderNTAdditionalFields32 = 68;
        internal const int SizeofOptionalHeaderNTAdditionalFields64 = 88;
        internal const int NumberofOptionalHeaderDirectoryEntries = 16;
        internal const int SizeofOptionalHeaderDirectoriesEntries = 64;
        internal const int SizeofSectionHeader = 40;
        internal const int SizeofSectionName = 8;
        internal const int SizeofResourceDirectory = 16;
        internal const int SizeofResourceDirectoryEntry = 8;
    }
}