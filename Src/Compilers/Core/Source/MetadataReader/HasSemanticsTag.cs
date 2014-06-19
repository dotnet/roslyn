using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class HasSemanticsTag
    {
        internal const int NumberOfBits = 1;
        internal const uint LargeRowSize = 0x00000001 << (16 - HasSemanticsTag.NumberOfBits);
        internal const uint Event = 0x00000000;
        internal const uint Property = 0x00000001;
        internal const uint TagMask = 0x00000001;
        internal const TableMask TablesReferenced =
          TableMask.Event
          | TableMask.Property;
        internal static uint[] TagToTokenTypeArray = { TokenTypeIds.Event, TokenTypeIds.Property };
        internal static uint ConvertToToken(uint hasSemantic)
        {
            return HasSemanticsTag.TagToTokenTypeArray[hasSemantic & HasSemanticsTag.TagMask] | hasSemantic >> HasSemanticsTag.NumberOfBits;
        }

        internal static uint ConvertEventRowIdToTag(uint eventRowId)
        {
            return eventRowId << HasSemanticsTag.NumberOfBits | HasSemanticsTag.Event;
        }

        internal static uint ConvertPropertyRowIdToTag(uint propertyRowId)
        {
            return propertyRowId << HasSemanticsTag.NumberOfBits | HasSemanticsTag.Property;
        }
    }
}