using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal struct ResourceDataEntry
    {
        internal readonly int RVAToData;
        internal readonly int Size;
        internal readonly int CodePage;
        internal readonly int Reserved;

        internal ResourceDataEntry(
          int rvaToData,
          int size,
          int codePage,
          int reserved)
        {
            this.RVAToData = rvaToData;
            this.Size = size;
            this.CodePage = codePage;
            this.Reserved = reserved;
        }
    }
}