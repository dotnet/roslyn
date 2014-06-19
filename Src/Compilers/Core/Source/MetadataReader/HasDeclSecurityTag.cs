using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class HasDeclSecurityTag
    {
        internal const int NumberOfBits = 2;
        internal const uint LargeRowSize = 0x00000001 << (16 - HasDeclSecurityTag.NumberOfBits);
        internal const uint TypeDef = 0x00000000;
        internal const uint Method = 0x00000001;
        internal const uint Assembly = 0x00000002;
        internal const uint TagMask = 0x00000003;
        internal const TableMask TablesReferenced =
          TableMask.TypeDef
          | TableMask.Method
          | TableMask.Assembly;
        internal static uint[] TagToTokenTypeArray = { TokenTypeIds.TypeDef, TokenTypeIds.MethodDef, TokenTypeIds.Assembly };
        internal static uint ConvertToToken(uint hasDeclSecurity)
        {
            return HasDeclSecurityTag.TagToTokenTypeArray[hasDeclSecurity & HasDeclSecurityTag.TagMask] | hasDeclSecurity >> HasDeclSecurityTag.NumberOfBits;
        }

        internal static uint ConvertToTag(uint token)
        {
            uint tokenKind = token & TokenTypeIds.TokenTypeMask;
            uint rowId = token & TokenTypeIds.RIDMask;
            if (tokenKind == TokenTypeIds.TypeDef)
            {
                return rowId << HasDeclSecurityTag.NumberOfBits | HasDeclSecurityTag.TypeDef;
            }
            else if (tokenKind == TokenTypeIds.MethodDef)
            {
                return rowId << HasDeclSecurityTag.NumberOfBits | HasDeclSecurityTag.Method;
            }
            else if (tokenKind == TokenTypeIds.Assembly)
            {
                return rowId << HasDeclSecurityTag.NumberOfBits | HasDeclSecurityTag.Assembly;
            }

            return 0;
        }
    }
}