using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal struct MetadataTableHeader
    {
        internal uint Reserved;
        internal byte MajorVersion;
        internal byte MinorVersion;
        internal HeapSizeFlag HeapSizeFlags;
        internal byte RowId;
        internal TableMask ValidTables;
        internal TableMask SortedTables;

        // Helper methods
        internal int GetNumberOfTablesPresent()
        {
            const ulong MASK_01010101010101010101010101010101 = 0x5555555555555555UL;
            const ulong MASK_00110011001100110011001100110011 = 0x3333333333333333UL;
            const ulong MASK_00001111000011110000111100001111 = 0x0F0F0F0F0F0F0F0FUL;
            const ulong MASK_00000000111111110000000011111111 = 0x00FF00FF00FF00FFUL;
            const ulong MASK_00000000000000001111111111111111 = 0x0000FFFF0000FFFFUL;
            const ulong MASK_11111111111111111111111111111111 = 0x00000000FFFFFFFFUL;
            ulong count = (ulong)this.ValidTables;
            count = (count & MASK_01010101010101010101010101010101) + ((count >> 1) & MASK_01010101010101010101010101010101);
            count = (count & MASK_00110011001100110011001100110011) + ((count >> 2) & MASK_00110011001100110011001100110011);
            count = (count & MASK_00001111000011110000111100001111) + ((count >> 4) & MASK_00001111000011110000111100001111);
            count = (count & MASK_00000000111111110000000011111111) + ((count >> 8) & MASK_00000000111111110000000011111111);
            count = (count & MASK_00000000000000001111111111111111) + ((count >> 16) & MASK_00000000000000001111111111111111);
            count = (count & MASK_11111111111111111111111111111111) + ((count >> 32) & MASK_11111111111111111111111111111111);
            return (int)count;
        }
    }
}