using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class TypeOrMethodDefTag
    {
        internal const int NumberOfBits = 1;
        internal const uint LargeRowSize = 0x00000001 << (16 - TypeOrMethodDefTag.NumberOfBits);
        internal const uint TypeDef = 0x00000000;
        internal const uint MethodDef = 0x00000001;
        internal const uint TagMask = 0x0000001;
        internal static uint[] TagToTokenTypeArray = { TokenTypeIds.TypeDef, TokenTypeIds.MethodDef };
        internal const TableMask TablesReferenced =
          TableMask.TypeDef
          | TableMask.Method;
        internal static uint ConvertToToken(uint typeOrMethodDef)
        {
            return TypeOrMethodDefTag.TagToTokenTypeArray[typeOrMethodDef & TypeOrMethodDefTag.TagMask] | typeOrMethodDef >> TypeOrMethodDefTag.NumberOfBits;
        }

        internal static uint ConvertTypeDefRowIdToTag(uint typeDefRowId)
        {
            return typeDefRowId << TypeOrMethodDefTag.NumberOfBits | TypeOrMethodDefTag.TypeDef;
        }

        internal static uint ConvertMethodDefRowIdToTag(uint methodDefRowId)
        {
            return methodDefRowId << TypeOrMethodDefTag.NumberOfBits | TypeOrMethodDefTag.MethodDef;
        }
    }
}