using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class HasFieldMarshalTag
    {
        internal const int NumberOfBits = 1;
        internal const uint LargeRowSize = 0x00000001 << (16 - HasFieldMarshalTag.NumberOfBits);
        internal const uint Field = 0x00000000;
        internal const uint Param = 0x00000001;
        internal const uint TagMask = 0x00000001;
        internal const TableMask TablesReferenced =
          TableMask.Field
          | TableMask.Param;
        internal static uint[] TagToTokenTypeArray = { TokenTypeIds.FieldDef, TokenTypeIds.ParamDef };
        internal static uint ConvertToToken(uint hasFieldMarshal)
        {
            return HasFieldMarshalTag.TagToTokenTypeArray[hasFieldMarshal & HasFieldMarshalTag.TagMask] | hasFieldMarshal >> HasFieldMarshalTag.NumberOfBits;
        }

        internal static uint ConvertToTag(uint token)
        {
            uint tokenKind = token & TokenTypeIds.TokenTypeMask;
            uint rowId = token & TokenTypeIds.RIDMask;
            if (tokenKind == TokenTypeIds.FieldDef)
            {
                return rowId << HasFieldMarshalTag.NumberOfBits | HasFieldMarshalTag.Field;
            }
            else if (tokenKind == TokenTypeIds.ParamDef)
            {
                return rowId << HasFieldMarshalTag.NumberOfBits | HasFieldMarshalTag.Param;
            }

            return 0;
        }
    }
}