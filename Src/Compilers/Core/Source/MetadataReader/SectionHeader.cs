using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal struct SectionHeader
    {
        internal string Name;
        internal int VirtualSize;
        internal int VirtualAddress;
        internal int SizeOfRawData;
        internal int OffsetToRawData;
        internal int RVAToRelocations;
        internal int PointerToLineNumbers;
        internal ushort NumberOfRelocations;
        internal ushort NumberOfLineNumbers;
        internal SectionCharacteristics SectionCharacteristics;
    }
}