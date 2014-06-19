using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class TypeDefOrRefTag
    {
        internal const int NumberOfBits = 2;
        internal const uint LargeRowSize = 0x00000001 << (16 - TypeDefOrRefTag.NumberOfBits);
        internal const uint TypeDef = 0x00000000;
        internal const uint TypeRef = 0x00000001;
        internal const uint TypeSpec = 0x00000002;
        internal const uint TagMask = 0x00000003;
        internal static uint[] TagToTokenTypeArray = { TokenTypeIds.TypeDef, TokenTypeIds.TypeRef, TokenTypeIds.TypeSpec };
        internal const TableMask TablesReferenced =
          TableMask.TypeDef
          | TableMask.TypeRef
          | TableMask.TypeSpec;
        internal static uint ConvertToToken(uint typeDefOrRefTag)
        {
            return TypeDefOrRefTag.TagToTokenTypeArray[typeDefOrRefTag & TypeDefOrRefTag.TagMask] | typeDefOrRefTag >> TypeDefOrRefTag.NumberOfBits;
        }
    }
}