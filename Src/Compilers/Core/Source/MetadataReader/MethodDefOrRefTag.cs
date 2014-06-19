using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class MethodDefOrRefTag
    {
        internal const int NumberOfBits = 1;
        internal const uint LargeRowSize = 0x00000001 << (16 - MethodDefOrRefTag.NumberOfBits);
        internal const uint Method = 0x00000000;
        internal const uint MemberRef = 0x00000001;
        internal const uint TagMask = 0x00000001;
        internal const TableMask TablesReferenced =
          TableMask.Method
          | TableMask.MemberRef;
        internal static uint[] TagToTokenTypeArray = { TokenTypeIds.MethodDef, TokenTypeIds.MemberRef };
        internal static uint ConvertToToken(uint methodDefOrRef)
        {
            return MethodDefOrRefTag.TagToTokenTypeArray[methodDefOrRef & MethodDefOrRefTag.TagMask] | methodDefOrRef >> MethodDefOrRefTag.NumberOfBits;
        }
    }
}