using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal struct COFFFileHeader
    {
        internal Machine Machine;
        internal short NumberOfSections;
        internal int TimeDateStamp;
        internal int PointerToSymbolTable;
        internal int NumberOfSymbols;
        internal short SizeOfOptionalHeader;
        internal Characteristics Characteristics;
    }
}