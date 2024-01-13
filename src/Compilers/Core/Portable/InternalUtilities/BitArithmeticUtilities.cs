// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Roslyn.Utilities
{
    internal static class BitArithmeticUtilities
    {
        public static int CountBits(int v)
        {
            return CountBits(unchecked((uint)v));
        }

        public static int CountBits(uint v)
        {
            unchecked
            {
                v -= ((v >> 1) & 0x55555555u);
                v = (v & 0x33333333u) + ((v >> 2) & 0x33333333u);
                return (int)((v + (v >> 4) & 0xF0F0F0Fu) * 0x1010101u) >> 24;
            }
        }

        public static int CountBits(long v)
        {
            return CountBits(unchecked((ulong)v));
        }

        public static int CountBits(ulong v)
        {
            unchecked
            {
                const ulong MASK_01010101010101010101010101010101 = 0x5555555555555555UL;
                const ulong MASK_00110011001100110011001100110011 = 0x3333333333333333UL;
                const ulong MASK_00001111000011110000111100001111 = 0x0F0F0F0F0F0F0F0FUL;
                const ulong MASK_00000000111111110000000011111111 = 0x00FF00FF00FF00FFUL;
                const ulong MASK_00000000000000001111111111111111 = 0x0000FFFF0000FFFFUL;
                const ulong MASK_11111111111111111111111111111111 = 0x00000000FFFFFFFFUL;
                v = (v & MASK_01010101010101010101010101010101) + ((v >> 1) & MASK_01010101010101010101010101010101);
                v = (v & MASK_00110011001100110011001100110011) + ((v >> 2) & MASK_00110011001100110011001100110011);
                v = (v & MASK_00001111000011110000111100001111) + ((v >> 4) & MASK_00001111000011110000111100001111);
                v = (v & MASK_00000000111111110000000011111111) + ((v >> 8) & MASK_00000000111111110000000011111111);
                v = (v & MASK_00000000000000001111111111111111) + ((v >> 16) & MASK_00000000000000001111111111111111);
                v = (v & MASK_11111111111111111111111111111111) + ((v >> 32) & MASK_11111111111111111111111111111111);
                return (int)v;
            }
        }

        internal static uint Align(uint position, uint alignment)
        {
            Debug.Assert(CountBits(alignment) == 1);

            uint result = position & ~(alignment - 1);
            if (result == position)
            {
                return result;
            }

            return result + alignment;
        }

        internal static int Align(int position, int alignment)
        {
            Debug.Assert(position >= 0 && alignment > 0);
            Debug.Assert(CountBits(alignment) == 1);

            int result = position & ~(alignment - 1);
            if (result == position)
            {
                return result;
            }

            return result + alignment;
        }
    }
}
