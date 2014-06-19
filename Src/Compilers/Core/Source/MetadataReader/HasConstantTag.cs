using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class HasConstantTag
    {
        internal const int NumberOfBits = 2;
        internal const uint LargeRowSize = 0x00000001 << (16 - HasConstantTag.NumberOfBits);
        internal const uint Field = 0x00000000;
        internal const uint Param = 0x00000001;
        internal const uint Property = 0x00000002;
        internal const uint TagMask = 0x00000003;
        internal const TableMask TablesReferenced =
          TableMask.Field
          | TableMask.Param
          | TableMask.Property;
        internal static uint[] TagToTokenTypeArray = { TokenTypeIds.FieldDef, TokenTypeIds.ParamDef, TokenTypeIds.Property };
        internal static uint ConvertToToken(uint hasConstant)
        {
            return HasConstantTag.TagToTokenTypeArray[hasConstant & HasConstantTag.TagMask] | hasConstant >> HasConstantTag.NumberOfBits;
        }

        internal static uint ConvertToTag(uint token)
        {
            uint tokenKind = token & TokenTypeIds.TokenTypeMask;
            uint rowId = token & TokenTypeIds.RIDMask;
            if (tokenKind == TokenTypeIds.FieldDef)
            {
                return rowId << HasConstantTag.NumberOfBits | HasConstantTag.Field;
            }
            else if (tokenKind == TokenTypeIds.ParamDef)
            {
                return rowId << HasConstantTag.NumberOfBits | HasConstantTag.Param;
            }
            else if (tokenKind == TokenTypeIds.Property)
            {
                return rowId << HasConstantTag.NumberOfBits | HasConstantTag.Property;
            }

            return 0;
        }
    }
}