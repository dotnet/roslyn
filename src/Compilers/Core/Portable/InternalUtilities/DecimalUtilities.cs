// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.Utilities
{
    internal static class DecimalUtilities
    {
        public static int GetScale(this decimal value)
        {
            return unchecked((byte)(decimal.GetBits(value)[3] >> 16));
        }

        public static void GetBits(this decimal value, out bool isNegative, out byte scale, out uint low, out uint mid, out uint high)
        {
            int[] bits = decimal.GetBits(value);

            // The return value is a four-element array of 32-bit signed integers.
            // The first, second, and third elements of the returned array contain the low, middle, and high 32 bits of the 96-bit integer number.
            low = unchecked((uint)bits[0]);
            mid = unchecked((uint)bits[1]);
            high = unchecked((uint)bits[2]);

            // The fourth element of the returned array contains the scale factor and sign. It consists of the following parts:
            // Bits 0 to 15, the lower word, are unused and must be zero.
            // Bits 16 to 23 must contain an exponent between 0 and 28, which indicates the power of 10 to divide the integer number.
            // Bits 24 to 30 are unused and must be zero.
            // Bit 31 contains the sign; 0 meaning positive, and 1 meaning negative.
            scale = unchecked((byte)(bits[3] >> 16));
            isNegative = (bits[3] & 0x80000000) != 0;
        }
    }
}
