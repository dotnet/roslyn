using System;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata.Ecma335.Tokens;
using Microsoft.CodeAnalysis.MetadataReader.PEFileFlags;
using Microsoft.CodeAnalysis.MetadataReader.UtilityDataStructures;

namespace Microsoft.CodeAnalysis.MetadataReader.PESignatures
{
    internal static class SignatureHelpers
    {
        private static readonly uint[] corEncodeTokenArray = new uint[] { TokenTypeIds.TypeDef, TokenTypeIds.TypeRef, TokenTypeIds.TypeSpec };

        internal static MetadataToken DecodeToken(ref MemoryReader ppSig)
        {
            uint value = ppSig.ReadCompressedUInt32();
            if (value == MemoryReader.InvalidCompressedInteger)
            {
                return default(MetadataToken);
            }

            uint tokenType = corEncodeTokenArray[value & 0x3];
            return MetadataTokens.CreateHandle(tokenType | (value >> 2));
        }
    }
}