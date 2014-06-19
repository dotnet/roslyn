using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class MemberForwardedTag
    {
        internal const int NumberOfBits = 1;
        internal const uint LargeRowSize = 0x00000001 << (16 - MemberForwardedTag.NumberOfBits);
        internal const uint Field = 0x00000000;
        internal const uint Method = 0x00000001;
        internal const uint TagMask = 0x00000001;
        internal const TableMask TablesReferenced =
          TableMask.Field
          | TableMask.Method;
        internal static uint[] TagToTokenTypeArray = { TokenTypeIds.FieldDef, TokenTypeIds.MethodDef };
        internal static uint ConvertToToken(uint memberForwarded)
        {
            return MemberForwardedTag.TagToTokenTypeArray[memberForwarded & MethodDefOrRefTag.TagMask] | memberForwarded >> MethodDefOrRefTag.NumberOfBits;
        }

        internal static uint ConvertMethodDefRowIdToTag(uint methodDefRowId)
        {
            return methodDefRowId << MemberForwardedTag.NumberOfBits | MemberForwardedTag.Method;
        }
#if false
    internal static uint ConvertFieldDefRowIdToTag(uint fieldDefRowId) {
      return fieldDefRowId << MemberForwardedTag.NumberOfBits | MemberForwardedTag.Field;
    }
#endif
    }
}