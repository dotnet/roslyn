using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class CustomAttributeTypeTag
    {
        internal const int NumberOfBits = 3;
        internal const uint LargeRowSize = 0x00000001 << (16 - CustomAttributeTypeTag.NumberOfBits);
        internal const uint Method = 0x00000002;
        internal const uint MemberRef = 0x00000003;
        internal const uint TagMask = 0x0000007;
        internal static uint[] TagToTokenTypeArray = { 0, 0, TokenTypeIds.MethodDef, TokenTypeIds.MemberRef, 0 };
        internal const TableMask TablesReferenced =
          TableMask.Method
          | TableMask.MemberRef;
        internal static uint ConvertToToken(uint customAttributeType)
        {
            return CustomAttributeTypeTag.TagToTokenTypeArray[customAttributeType & CustomAttributeTypeTag.TagMask] | customAttributeType >> CustomAttributeTypeTag.NumberOfBits;
        }
    }
}