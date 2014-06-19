using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal struct OptionalHeaderStandardFields
    {
        internal PEMagic PEMagic;
        internal byte MajorLinkerVersion;
        internal byte MinorLinkerVersion;
        internal int SizeOfCode;
        internal int SizeOfInitializedData;
        internal int SizeOfUninitializedData;
        internal int RVAOfEntryPoint;
        internal int BaseOfCode;
        internal int BaseOfData;
    }
}