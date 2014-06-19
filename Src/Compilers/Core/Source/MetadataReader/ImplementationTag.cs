using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class ImplementationTag
    {
        internal const int NumberOfBits = 2;
        internal const uint LargeRowSize = 0x00000001 << (16 - ImplementationTag.NumberOfBits);
        internal const uint File = 0x00000000;
        internal const uint AssemblyRef = 0x00000001;
        internal const uint ExportedType = 0x00000002;
        internal const uint TagMask = 0x00000003;
        internal static uint[] TagToTokenTypeArray = { TokenTypeIds.File, TokenTypeIds.AssemblyRef, TokenTypeIds.ExportedType };
        internal const TableMask TablesReferenced =
          TableMask.File
          | TableMask.AssemblyRef
          | TableMask.ExportedType;
        internal static uint ConvertToToken(uint implementation)
        {
            if (implementation == 0)
            {
                return 0;
            }

            return ImplementationTag.TagToTokenTypeArray[implementation & ImplementationTag.TagMask] | implementation >> ImplementationTag.NumberOfBits;
        }
    }
}