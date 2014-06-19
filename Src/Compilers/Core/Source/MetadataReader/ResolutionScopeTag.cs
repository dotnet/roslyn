using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class ResolutionScopeTag
    {
        internal const int NumberOfBits = 2;
        internal const uint LargeRowSize = 0x00000001 << (16 - ResolutionScopeTag.NumberOfBits);
        internal const uint Module = 0x00000000;
        internal const uint ModuleRef = 0x00000001;
        internal const uint AssemblyRef = 0x00000002;
        internal const uint TypeRef = 0x00000003;
        internal const uint TagMask = 0x00000003;
        internal static uint[] TagToTokenTypeArray = { TokenTypeIds.Module, TokenTypeIds.ModuleRef, TokenTypeIds.AssemblyRef, TokenTypeIds.TypeRef };
        internal const TableMask TablesReferenced =
          TableMask.Module
          | TableMask.ModuleRef
          | TableMask.AssemblyRef
          | TableMask.TypeRef;
        internal static uint ConvertToToken(uint resolutionScope)
        {
            return ResolutionScopeTag.TagToTokenTypeArray[resolutionScope & ResolutionScopeTag.TagMask] | resolutionScope >> ResolutionScopeTag.NumberOfBits;
        }
    }
}