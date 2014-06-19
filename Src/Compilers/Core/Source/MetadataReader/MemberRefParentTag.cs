using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class MemberRefParentTag
    {
        internal const int NumberOfBits = 3;
        internal const uint LargeRowSize = 0x00000001 << (16 - MemberRefParentTag.NumberOfBits);
        internal const uint TypeDef = 0x00000000;
        internal const uint TypeRef = 0x00000001;
        internal const uint ModuleRef = 0x00000002;
        internal const uint Method = 0x00000003;
        internal const uint TypeSpec = 0x00000004;
        internal const uint TagMask = 0x00000007;
        internal const TableMask TablesReferenced =
          TableMask.TypeDef
          | TableMask.TypeRef
          | TableMask.ModuleRef
          | TableMask.Method
          | TableMask.TypeSpec;
        internal static uint[] TagToTokenTypeArray =
        {
            TokenTypeIds.TypeDef, TokenTypeIds.TypeRef, TokenTypeIds.ModuleRef,
            TokenTypeIds.MethodDef, TokenTypeIds.TypeSpec
        };

        internal static uint ConvertToToken(uint memberRef)
        {
            return MemberRefParentTag.TagToTokenTypeArray[memberRef & MemberRefParentTag.TagMask] | memberRef >> MemberRefParentTag.NumberOfBits;
        }
    }
}