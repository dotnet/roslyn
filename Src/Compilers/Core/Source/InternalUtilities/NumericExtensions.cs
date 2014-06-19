using System;

namespace Roslyn.Utilities
{
    internal static class NumericExtensions
    {
        // Suggested by Jon Skeet.
        private static readonly long DoubleNegativeZeroBits = BitConverter.DoubleToInt64Bits(-0d);

        public static bool IsNegativeZero(this double d)
        {
            return BitConverter.DoubleToInt64Bits(d) == DoubleNegativeZeroBits;
        }

        public static bool IsNegativeZero(this float f)
        {
            return BitConverter.DoubleToInt64Bits((double)f) == DoubleNegativeZeroBits;
        }

        public static bool IsNegativeZero(this decimal d)
        {
            return BitConverter.DoubleToInt64Bits((double)d) == DoubleNegativeZeroBits;
        }
    }
}