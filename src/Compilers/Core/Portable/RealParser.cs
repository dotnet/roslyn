// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A set of utilities for converting from a decimal floating-point literal string to its IEEE float
    /// or double representation, which considers all digits significant and correctly rounds according to
    /// the IEEE round-to-nearest-ties-to-even mode. This code does not support a leading sign character,
    /// as that is not part of the C# or VB floating-point literal lexical syntax.
    /// 
    /// If you change this code, please run the set of long-running random tests in the solution
    /// RandomRealParserTests.sln. That solution is not included in Roslyn.slnx as it is Windows-specific.
    /// </summary>
    internal static class RealParser
    {
        /// <summary>
        /// Try parsing a correctly-formatted double floating-point literal into the nearest representable double
        /// using IEEE round-to-nearest-ties-to-even rounding mode. Behavior is not defined for inputs that are
        /// not valid C# floating-point literals.
        /// </summary>
        /// <param name="s">The decimal floating-point constant's string</param>
        /// <param name="d">The nearest double value, if conversion succeeds</param>
        /// <returns>True if the input was converted; false if there was an overflow</returns>
        public static bool TryParseDouble(string s, out double d)
        {
            var str = DecimalFloatingPointString.FromSource(s);
            var dbl = DoubleFloatingPointType.Instance;
            ulong result;
            var status = RealParser.ConvertDecimalToFloatingPointBits(str, dbl, out result);
            d = BitConverter.Int64BitsToDouble((long)result);
            return status != Status.Overflow;
        }

        /// <summary>
        /// Try parsing a correctly-formatted float floating-point literal into the nearest representable float
        /// using IEEE round-to-nearest-ties-to-even rounding mode. Behavior is not defined for inputs that are
        /// not valid C# floating-point literals.
        /// </summary>
        /// <param name="s">The float floating-point constant's string</param>
        /// <param name="f">The nearest float value, if conversion succeeds</param>
        /// <returns>True if the input was converted; false if there was an overflow</returns>
        public static bool TryParseFloat(string s, out float f)
        {
            var str = DecimalFloatingPointString.FromSource(s);
            var dbl = FloatFloatingPointType.Instance;
            ulong result;
            var status = RealParser.ConvertDecimalToFloatingPointBits(str, dbl, out result);
            f = Int32BitsToFloat((uint)result);
            return status != Status.Overflow;
        }

        private static readonly BigInteger s_bigZero = BigInteger.Zero;
        private static readonly BigInteger s_bigOne = BigInteger.One;
        private static readonly BigInteger s_bigTwo = new BigInteger(2);
        private static readonly BigInteger s_bigTen = new BigInteger(10);

        /// <summary>
        /// Properties of an IEEE floating-point representation.
        /// </summary>
        private abstract class FloatingPointType
        {
            public abstract ushort DenormalMantissaBits { get; }
            public ushort NormalMantissaBits => (ushort)(DenormalMantissaBits + 1); // we get an extra (hidden) bit for normal mantissas
            public abstract ushort ExponentBits { get; }
            public int MinBinaryExponent => 1 - MaxBinaryExponent;
            public abstract int MaxBinaryExponent { get; }
            public int OverflowDecimalExponent => (MaxBinaryExponent + 2 * NormalMantissaBits) / 3;
            public abstract int ExponentBias { get; }
            public ulong DenormalMantissaMask => (1UL << (DenormalMantissaBits)) - 1;
            public ulong NormalMantissaMask => (1UL << NormalMantissaBits) - 1;
            public abstract ulong Zero { get; }
            public abstract ulong Infinity { get; }

            /// <summary>
            /// Converts the floating point value 0.mantissa * 2^exponent into the  
            /// correct form for the FloatingPointType and stores the bits of the resulting value  
            /// into the result object.  
            /// The caller must ensure that the mantissa and exponent are correctly computed  
            /// such that either [1] the most significant bit of the mantissa is in the  
            /// correct position for the FloatingType, or [2] the exponent has been correctly  
            /// adjusted to account for the shift of the mantissa that will be required.  
            ///  
            /// This function correctly handles range errors and stores a zero or infinity in  
            /// the result object on underflow and overflow errors, respectively.  This  
            /// function correctly forms denormal numbers when required.  
            ///  
            /// If the provided mantissa has more bits of precision than can be stored in the  
            /// result object, the mantissa is rounded to the available precision.  Thus, if  
            /// possible, the caller should provide a mantissa with at least one more bit of  
            /// precision than is required, to ensure that the mantissa is correctly rounded.  
            /// (The caller should not round the mantissa before calling this function.)  
            /// </summary>
            /// <param name="initialMantissa">The bits of the mantissa</param>
            /// <param name="initialExponent">The exponent</param>
            /// <param name="hasZeroTail">Whether there are any nonzero bits past the supplied mantissa</param>
            /// <param name="result">Where the bits of the floating-point number are stored</param>
            /// <returns>A status indicating whether the conversion succeeded and why</returns>
            public Status AssembleFloatingPointValue(
                ulong initialMantissa,
                int initialExponent,
                bool hasZeroTail,
                out ulong result)
            {
                // number of bits by which we must adjust the mantissa to shift it into the  
                // correct position, and compute the resulting base two exponent for the  
                // normalized mantissa:  
                uint initialMantissaBits = CountSignificantBits(initialMantissa);
                int normalMantissaShift = this.NormalMantissaBits - (int)initialMantissaBits;
                int normalExponent = initialExponent - normalMantissaShift;

                ulong mantissa = initialMantissa;
                int exponent = normalExponent;

                if (normalExponent > this.MaxBinaryExponent)
                {
                    // The exponent is too large to be represented by the floating point  
                    // type; report the overflow condition:
                    result = this.Infinity;
                    return Status.Overflow;
                }
                else if (normalExponent < this.MinBinaryExponent)
                {
                    // The exponent is too small to be represented by the floating point  
                    // type as a normal value, but it may be representable as a denormal  
                    // value.  Compute the number of bits by which we need to shift the  
                    // mantissa in order to form a denormal number.  (The subtraction of  
                    // an extra 1 is to account for the hidden bit of the mantissa that  
                    // is not available for use when representing a denormal.)  
                    int denormalMantissaShift =
                        normalMantissaShift +
                        normalExponent +
                        this.ExponentBias -
                        1;

                    // Denormal values have an exponent of zero, so the debiased exponent is  
                    // the negation of the exponent bias:  
                    exponent = -this.ExponentBias;

                    if (denormalMantissaShift < 0)
                    {
                        // Use two steps for right shifts:  for a shift of N bits, we first  
                        // shift by N-1 bits, then shift the last bit and use its value to  
                        // round the mantissa.  
                        mantissa = RightShiftWithRounding(mantissa, -denormalMantissaShift, hasZeroTail);

                        // If the mantissa is now zero, we have underflowed:  
                        if (mantissa == 0)
                        {
                            result = this.Zero;
                            return Status.Underflow;
                        }

                        // When we round the mantissa, the result may be so large that the  
                        // number becomes a normal value.  For example, consider the single  
                        // precision case where the mantissa is 0x01ffffff and a right shift  
                        // of 2 is required to shift the value into position. We perform the  
                        // shift in two steps:  we shift by one bit, then we shift again and  
                        // round using the dropped bit.  The initial shift yields 0x00ffffff.  
                        // The rounding shift then yields 0x007fffff and because the least  
                        // significant bit was 1, we add 1 to this number to round it.  The  
                        // final result is 0x00800000.  
                        //  
                        // 0x00800000 is 24 bits, which is more than the 23 bits available  
                        // in the mantissa.  Thus, we have rounded our denormal number into  
                        // a normal number.  
                        //  
                        // We detect this case here and re-adjust the mantissa and exponent  
                        // appropriately, to form a normal number:  
                        if (mantissa > this.DenormalMantissaMask)
                        {
                            // We add one to the denormal_mantissa_shift to account for the  
                            // hidden mantissa bit (we subtracted one to account for this bit  
                            // when we computed the denormal_mantissa_shift above).  
                            exponent =
                                initialExponent -
                                (denormalMantissaShift + 1) -
                                normalMantissaShift;
                        }
                    }
                    else
                    {
                        mantissa <<= denormalMantissaShift;
                    }
                }
                else
                {
                    if (normalMantissaShift < 0)
                    {
                        // Use two steps for right shifts:  for a shift of N bits, we first  
                        // shift by N-1 bits, then shift the last bit and use its value to  
                        // round the mantissa.  
                        mantissa = RightShiftWithRounding(mantissa, -normalMantissaShift, hasZeroTail);

                        // When we round the mantissa, it may produce a result that is too  
                        // large.  In this case, we divide the mantissa by two and increment  
                        // the exponent (this does not change the value).  
                        if (mantissa > this.NormalMantissaMask)
                        {
                            mantissa >>= 1;
                            ++exponent;

                            // The increment of the exponent may have generated a value too  
                            // large to be represented.  In this case, report the overflow:  
                            if (exponent > this.MaxBinaryExponent)
                            {
                                result = this.Infinity;
                                return Status.Overflow;
                            }
                        }
                    }
                    else if (normalMantissaShift > 0)
                    {
                        mantissa <<= normalMantissaShift;
                    }
                }

                // Unset the hidden bit in the mantissa and assemble the floating point value  
                // from the computed components:  
                mantissa &= this.DenormalMantissaMask;

                Debug.Assert((DenormalMantissaMask & (1UL << DenormalMantissaBits)) == 0);
                ulong shiftedExponent = ((ulong)(exponent + this.ExponentBias)) << DenormalMantissaBits;
                Debug.Assert((shiftedExponent & DenormalMantissaMask) == 0);
                Debug.Assert((mantissa & ~DenormalMantissaMask) == 0);
                Debug.Assert((shiftedExponent & ~(((1UL << this.ExponentBits) - 1) << DenormalMantissaBits)) == 0); // exponent fits in its place
                result = shiftedExponent | mantissa;
                return Status.OK;
            }
        }

        /// <summary>
        /// Properties of a C# float.
        /// </summary>
        private sealed class FloatFloatingPointType : FloatingPointType
        {
            public static FloatFloatingPointType Instance = new FloatFloatingPointType();
            private FloatFloatingPointType() { }
            public override ushort DenormalMantissaBits => 23;
            public override ushort ExponentBits => 8;
            public override int MaxBinaryExponent => 127;
            public override int ExponentBias => 127;
            public override ulong Zero => FloatToInt32Bits(0.0f);
            public override ulong Infinity => FloatToInt32Bits(float.PositiveInfinity);
        }

        /// <summary>
        /// Properties of a C# double.
        /// </summary>
        private sealed class DoubleFloatingPointType : FloatingPointType
        {
            public static DoubleFloatingPointType Instance = new DoubleFloatingPointType();
            private DoubleFloatingPointType() { }
            public override ushort DenormalMantissaBits => 52;
            public override ushort ExponentBits => 11;
            public override int MaxBinaryExponent => 1023;
            public override int ExponentBias => 1023;
            public override ulong Zero => (ulong)BitConverter.DoubleToInt64Bits(0.0d);
            public override ulong Infinity => (ulong)BitConverter.DoubleToInt64Bits(double.PositiveInfinity);
        }

        /// <summary>
        /// This type is used to hold a partially-parsed string representation of a  
        /// floating point number.  The number is stored in the following form:  
        ///  <pre>
        ///     0.Mantissa * 10^Exponent
        ///  </pre>
        /// The Mantissa buffer stores the mantissa digits as characters in a string.  
        /// The MantissaCount gives the number of digits present in the Mantissa buffer.
        /// There shall be neither leading nor trailing zero digits in the Mantissa.
        /// Note that this represents only nonnegative floating-point literals; the
        /// negative sign in C# and VB is actually a separate unary negation operator.
        /// </summary>
        [DebuggerDisplay("0.{Mantissa}e{Exponent}")]
        private struct DecimalFloatingPointString
        {
            public int Exponent;
            public string Mantissa;
            public uint MantissaCount => (uint)Mantissa.Length;

            /// <summary>
            /// Create a DecimalFloatingPointString from a string representing a floating-point literal.
            /// </summary>
            /// <param name="source">The text of the floating-point literal</param>
            public static DecimalFloatingPointString FromSource(string source)
            {
                var mantissaBuilder = new StringBuilder();
                var exponent = 0;
                int i = 0;
                while (i < source.Length && source[i] == '0') i++;
                int skippedDecimals = 0;
                while (i < source.Length && source[i] >= '0' && source[i] <= '9')
                {
                    if (source[i] == '0')
                    {
                        skippedDecimals++;
                    }
                    else
                    {
                        mantissaBuilder.Append('0', skippedDecimals);
                        skippedDecimals = 0;
                        mantissaBuilder.Append(source[i]);
                    }
                    exponent++;
                    i++;
                }
                if (i < source.Length && source[i] == '.')
                {
                    i++;
                    while (i < source.Length && source[i] >= '0' && source[i] <= '9')
                    {
                        if (source[i] == '0')
                        {
                            skippedDecimals++;
                        }
                        else
                        {
                            mantissaBuilder.Append('0', skippedDecimals);
                            skippedDecimals = 0;
                            mantissaBuilder.Append(source[i]);
                        }
                        i++;
                    }
                }
                var result = default(DecimalFloatingPointString);
                result.Mantissa = mantissaBuilder.ToString();
                if (i < source.Length && (source[i] == 'e' || source[i] == 'E'))
                {
                    const int MAX_EXP = (1 << 30); // even playing ground
                    char exponentSign = '\0';
                    i++;
                    if (i < source.Length && (source[i] == '-' || source[i] == '+'))
                    {
                        exponentSign = source[i];
                        i++;
                    }
                    int firstExponent = i;
                    int lastExponent = i;
                    while (i < source.Length && source[i] >= '0' && source[i] <= '9') lastExponent = ++i;

                    int exponentMagnitude = 0;

                    if (int.TryParse(source.Substring(firstExponent, lastExponent - firstExponent), out exponentMagnitude) &&
                        exponentMagnitude <= MAX_EXP)
                    {
                        if (exponentSign == '-')
                        {
                            exponent -= exponentMagnitude;
                        }
                        else
                        {
                            exponent += exponentMagnitude;
                        }
                    }
                    else
                    {
                        exponent = exponentSign == '-' ? -MAX_EXP : MAX_EXP;
                    }
                }
                result.Exponent = exponent;
                return result;
            }
        }

        private enum Status
        {
            OK,
            NoDigits,
            Underflow,
            Overflow
        }

        /// <summary>
        /// Convert a DecimalFloatingPointString to the bits of the given floating-point type.
        /// </summary>
        private static Status ConvertDecimalToFloatingPointBits(DecimalFloatingPointString data, FloatingPointType type, out ulong result)
        {
            if (data.Mantissa.Length == 0)
            {
                result = type.Zero;
                return Status.NoDigits;
            }

            // To generate an N bit mantissa we require N + 1 bits of precision.  The  
            // extra bit is used to correctly round the mantissa (if there are fewer bits  
            // than this available, then that's totally okay; in that case we use what we  
            // have and we don't need to round).
            uint requiredBitsOfPrecision = (uint)type.NormalMantissaBits + 1;

            // The input is of the form 0.Mantissa x 10^Exponent, where 'Mantissa' are  
            // the decimal digits of the mantissa and 'Exponent' is the decimal exponent.  
            // We decompose the mantissa into two parts: an integer part and a fractional  
            // part.  If the exponent is positive, then the integer part consists of the  
            // first 'exponent' digits, or all present digits if there are fewer digits.  
            // If the exponent is zero or negative, then the integer part is empty.  In  
            // either case, the remaining digits form the fractional part of the mantissa.  
            uint positiveExponent = (uint)Math.Max(0, data.Exponent);
            uint integerDigitsPresent = Math.Min(positiveExponent, data.MantissaCount);
            uint integerDigitsMissing = positiveExponent - integerDigitsPresent;
            uint integerFirstIndex = 0;
            uint integerLastIndex = integerDigitsPresent;

            uint fractionalFirstIndex = integerLastIndex;
            uint fractionalLastIndex = data.MantissaCount;
            uint fractionalDigitsPresent = fractionalLastIndex - fractionalFirstIndex;

            // First, we accumulate the integer part of the mantissa into a big_integer:
            BigInteger integerValue = AccumulateDecimalDigitsIntoBigInteger(data, integerFirstIndex, integerLastIndex);

            if (integerDigitsMissing > 0)
            {
                if (integerDigitsMissing > type.OverflowDecimalExponent)
                {
                    result = type.Infinity;
                    return Status.Overflow;
                }

                MultiplyByPowerOfTen(ref integerValue, integerDigitsMissing);
            }

            // At this point, the integer_value contains the value of the integer part
            // of the mantissa.  If either [1] this number has more than the required
            // number of bits of precision or [2] the mantissa has no fractional part,
            // then we can assemble the result immediately:
            byte[] integerValueAsBytes;
            uint integerBitsOfPrecision = CountSignificantBits(integerValue, out integerValueAsBytes);
            if (integerBitsOfPrecision >= requiredBitsOfPrecision ||
                fractionalDigitsPresent == 0)
            {
                return ConvertBigIntegerToFloatingPointBits(
                    integerValueAsBytes,
                    integerBitsOfPrecision,
                    fractionalDigitsPresent != 0,
                    type,
                    out result);
            }

            // Otherwise, we did not get enough bits of precision from the integer part,  
            // and the mantissa has a fractional part.  We parse the fractional part of  
            // the mantissa to obtain more bits of precision.  To do this, we convert  
            // the fractional part into an actual fraction N/M, where the numerator N is  
            // computed from the digits of the fractional part, and the denominator M is   
            // computed as the power of 10 such that N/M is equal to the value of the  
            // fractional part of the mantissa.  

            uint fractionalDenominatorExponent = data.Exponent < 0
                ? fractionalDigitsPresent + (uint)-data.Exponent
                : fractionalDigitsPresent;
            if (integerBitsOfPrecision == 0 && (fractionalDenominatorExponent - (int)data.MantissaCount) > type.OverflowDecimalExponent)
            {
                // If there were any digits in the integer part, it is impossible to  
                // underflow (because the exponent cannot possibly be small enough),  
                // so if we underflow here it is a true underflow and we return zero.  
                result = type.Zero;
                return Status.Underflow;
            }

            BigInteger fractionalNumerator = AccumulateDecimalDigitsIntoBigInteger(data, fractionalFirstIndex, fractionalLastIndex);
            Debug.Assert(!fractionalNumerator.IsZero);

            BigInteger fractionalDenominator = s_bigOne;
            MultiplyByPowerOfTen(ref fractionalDenominator, fractionalDenominatorExponent);

            // Because we are using only the fractional part of the mantissa here, the  
            // numerator is guaranteed to be smaller than the denominator.  We normalize  
            // the fraction such that the most significant bit of the numerator is in  
            // the same position as the most significant bit in the denominator.  This  
            // ensures that when we later shift the numerator N bits to the left, we  
            // will produce N bits of precision.  
            uint fractionalNumeratorBits = CountSignificantBits(fractionalNumerator);
            uint fractionalDenominatorBits = CountSignificantBits(fractionalDenominator);

            uint fractionalShift = fractionalDenominatorBits > fractionalNumeratorBits
                ? fractionalDenominatorBits - fractionalNumeratorBits
                : 0;

            if (fractionalShift > 0)
            {
                ShiftLeft(ref fractionalNumerator, fractionalShift);
            }

            uint requiredFractionalBitsOfPrecision =
                requiredBitsOfPrecision -
                integerBitsOfPrecision;

            uint remainingBitsOfPrecisionRequired = requiredFractionalBitsOfPrecision;
            if (integerBitsOfPrecision > 0)
            {
                // If the fractional part of the mantissa provides no bits of precision  
                // and cannot affect rounding, we can just take whatever bits we got from  
                // the integer part of the mantissa.  This is the case for numbers like  
                // 5.0000000000000000000001, where the significant digits of the fractional  
                // part start so far to the right that they do not affect the floating  
                // point representation.  
                //  
                // If the fractional shift is exactly equal to the number of bits of  
                // precision that we require, then no fractional bits will be part of the  
                // result, but the result may affect rounding.  This is e.g. the case for  
                // large, odd integers with a fractional part greater than or equal to .5.  
                // Thus, we need to do the division to correctly round the result.  
                if (fractionalShift > remainingBitsOfPrecisionRequired)
                {
                    return ConvertBigIntegerToFloatingPointBits(
                        integerValueAsBytes,
                        integerBitsOfPrecision,
                        fractionalDigitsPresent != 0,
                        type,
                        out result);
                }

                remainingBitsOfPrecisionRequired -= fractionalShift;
            }

            // If there was no integer part of the mantissa, we will need to compute the  
            // exponent from the fractional part.  The fractional exponent is the power  
            // of two by which we must multiply the fractional part to move it into the  
            // range [1.0, 2.0).  This will either be the same as the shift we computed  
            // earlier, or one greater than that shift:  
            uint fractionalExponent = fractionalNumerator < fractionalDenominator
                ? fractionalShift + 1
                : fractionalShift;

            ShiftLeft(ref fractionalNumerator, remainingBitsOfPrecisionRequired);
            BigInteger fractionalRemainder;
            BigInteger bigFractionalMantissa = BigInteger.DivRem(fractionalNumerator, fractionalDenominator, out fractionalRemainder);
            ulong fractionalMantissa = (ulong)bigFractionalMantissa;

            bool hasZeroTail = fractionalRemainder.IsZero;

            // We may have produced more bits of precision than were required.  Check,  
            // and remove any "extra" bits:  
            uint fractionalMantissaBits = CountSignificantBits(fractionalMantissa);
            if (fractionalMantissaBits > requiredFractionalBitsOfPrecision)
            {
                int shift = (int)(fractionalMantissaBits - requiredFractionalBitsOfPrecision);
                hasZeroTail = hasZeroTail && (fractionalMantissa & ((1UL << shift) - 1)) == 0;
                fractionalMantissa >>= shift;
            }

            // Compose the mantissa from the integer and fractional parts:  
            Debug.Assert(integerBitsOfPrecision < 60); // we can use BigInteger's built-in conversion
            ulong integerMantissa = (ulong)integerValue;

            ulong completeMantissa =
                (integerMantissa << (int)requiredFractionalBitsOfPrecision) +
                fractionalMantissa;

            // Compute the final exponent:  
            // * If the mantissa had an integer part, then the exponent is one less than  
            //   the number of bits we obtained from the integer part.  (It's one less  
            //   because we are converting to the form 1.11111, with one 1 to the left  
            //   of the decimal point.)  
            // * If the mantissa had no integer part, then the exponent is the fractional  
            //   exponent that we computed.  
            // Then, in both cases, we subtract an additional one from the exponent, to  
            // account for the fact that we've generated an extra bit of precision, for  
            // use in rounding.  
            int finalExponent = integerBitsOfPrecision > 0
                ? (int)integerBitsOfPrecision - 2
                : -(int)(fractionalExponent) - 1;

            return type.AssembleFloatingPointValue(completeMantissa, finalExponent, hasZeroTail, out result);
        }

        /// <summary>
        /// This function is part of the fast track for integer floating point strings.  
        /// It takes an integer stored as an array of bytes (lsb first) and converts the value into its FloatingType  
        /// representation, storing the bits into "result".  If the value is not  
        /// representable, +/-infinity is stored and overflow is reported (since this  
        /// function only deals with integers, underflow is impossible).  
        /// </summary>
        /// <param name="integerValueAsBytes">the bits of the integer, least significant bits first</param>
        /// <param name="integerBitsOfPrecision">the number of bits of precision in integerValueAsBytes</param>
        /// <param name="hasNonzeroFractionalPart">whether there are nonzero digits after the decimal</param>
        /// <param name="type">the kind of real number to build</param>
        /// <param name="result">the result</param>
        /// <returns>An indicator of the kind of result</returns>
        private static Status ConvertBigIntegerToFloatingPointBits(byte[] integerValueAsBytes, uint integerBitsOfPrecision, bool hasNonzeroFractionalPart, FloatingPointType type, out ulong result)
        {
            int baseExponent = type.DenormalMantissaBits;
            int exponent;
            ulong mantissa;
            bool has_zero_tail = !hasNonzeroFractionalPart;
            int topElementIndex = ((int)integerBitsOfPrecision - 1) / 8;

            // The high-order byte of integerValueAsBytes might not have a full eight bits.  However,  
            // since the data are stored in quanta of 8 bits, and we really only need around 54  
            // bits of mantissa for a double (and fewer for a float), we can just assemble data  
            // from the eight high-order bytes and we will get between 59 and 64 bits, which is more  
            // than enough.
            int bottomElementIndex = Math.Max(0, topElementIndex - (64 / 8) + 1);
            exponent = baseExponent + bottomElementIndex * 8;
            mantissa = 0;
            for (int i = (int)topElementIndex; i >= bottomElementIndex; i--)
            {
                mantissa <<= 8;
                mantissa |= integerValueAsBytes[i];
            }
            for (int i = bottomElementIndex - 1; has_zero_tail && i >= 0; i--)
            {
                if (integerValueAsBytes[i] != 0) has_zero_tail = false;
            }

            return type.AssembleFloatingPointValue(mantissa, exponent, has_zero_tail, out result);
        }

        /// <summary>
        /// Parse a sequence of digits into a BigInteger.
        /// </summary>
        /// <param name="data">The DecimalFloatingPointString containing the digits in its Mantissa</param>
        /// <param name="integer_first_index">The index of the first digit to convert</param>
        /// <param name="integer_last_index">The index just past the last digit to convert</param>
        /// <returns>The BigInteger result</returns>
        private static BigInteger AccumulateDecimalDigitsIntoBigInteger(DecimalFloatingPointString data, uint integer_first_index, uint integer_last_index)
        {
            if (integer_first_index == integer_last_index) return s_bigZero;
            var valueString = data.Mantissa.Substring((int)integer_first_index, (int)(integer_last_index - integer_first_index));
            return BigInteger.Parse(valueString);
        }

        /// <summary>
        /// Return the number of significant bits set. 
        /// </summary>
        private static uint CountSignificantBits(ulong data)
        {
            uint result = 0;
            while (data != 0)
            {
                data >>= 1;
                result++;
            }

            return result;
        }

        /// <summary>
        /// Return the number of significant bits set. 
        /// </summary>
        private static uint CountSignificantBits(byte data)
        {
            uint result = 0;
            while (data != 0)
            {
                data >>= 1;
                result++;
            }

            return result;
        }

        /// <summary>
        /// Return the number of significant bits set. 
        /// </summary>
        private static uint CountSignificantBits(BigInteger data, out byte[] dataBytes)
        {
            if (data.IsZero)
            {
                dataBytes = new byte[1];
                return 0;
            }

            dataBytes = data.ToByteArray(); // the bits of the BigInteger, least significant bits first
            for (int i = dataBytes.Length - 1; i >= 0; i--)
            {
                var v = dataBytes[i];
                if (v != 0) return 8 * (uint)i + CountSignificantBits(v);
            }

            return 0;
        }

        /// <summary>
        /// Return the number of significant bits set. 
        /// </summary>
        private static uint CountSignificantBits(BigInteger data)
        {
            byte[] dataBytes;
            return CountSignificantBits(data, out dataBytes);
        }

        /// <summary>
        /// Computes value / 2^shift, then rounds the result according to the current  
        /// rounding mode.  By the time we call this function, we will already have  
        /// discarded most digits.  The caller must pass true for has_zero_tail if  
        /// all discarded bits were zeroes.  
        /// </summary>
        /// <param name="value">The value to shift</param>
        /// <param name="shift">The amount of shift</param>
        /// <param name="hasZeroTail">Whether there are any less significant nonzero bits in the value</param>
        /// <returns></returns>
        private static ulong RightShiftWithRounding(ulong value, int shift, bool hasZeroTail)
        {
            // If we'd need to shift further than it is possible to shift, the answer  
            // is always zero:  
            if (shift >= 64) return 0;

            ulong extraBitsMask = (1UL << (shift - 1)) - 1;
            ulong roundBitMask = (1UL << (shift - 1));
            ulong lsbBitMask = 1UL << shift;

            bool lsbBit = (value & lsbBitMask) != 0;
            bool roundBit = (value & roundBitMask) != 0;
            bool hasTailBits = !hasZeroTail || (value & extraBitsMask) != 0;

            return (value >> shift) + (ShouldRoundUp(lsbBit: lsbBit, roundBit: roundBit, hasTailBits: hasTailBits) ? 1UL : 0);
        }

        /// <summary>
        /// Determines whether a mantissa should be rounded up in the  
        /// round-to-nearest-ties-to-even mode given [1] the value of the least  
        /// significant bit of the mantissa, [2] the value of the next bit after  
        /// the least significant bit (the "round" bit) and [3] whether any  
        /// trailing bits after the round bit are set.  
        ///  
        /// The mantissa is treated as an unsigned integer magnitude.  
        ///  
        /// For this function, "round up" is defined as "increase the magnitude" of the  
        /// mantissa.
        /// </summary>
        /// <param name="lsbBit">the least-significant bit of the representable value</param>
        /// <param name="roundBit">the bit following the least-significant bit</param>
        /// <param name="hasTailBits">true if there are any (less significant) bits set following roundBit</param>
        /// <returns></returns>
        private static bool ShouldRoundUp(
            bool lsbBit,
            bool roundBit,
            bool hasTailBits)
        {
            // If there are insignificant set bits, we need to round to the  
            // nearest; there are two cases:  
            // we round up if either [1] the value is slightly greater than the midpoint  
            // between two exactly representable values or [2] the value is exactly the  
            // midpoint between two exactly representable values and the greater of the  
            // two is even (this is "round-to-even").  
            return roundBit && (hasTailBits || lsbBit);
        }

        /// <summary>
        /// Multiply a BigInteger by the given power of two.
        /// </summary>
        /// <param name="number">The BigInteger to multiply by a power of two and replace with the product</param>
        /// <param name="shift">The power of two to multiply it by</param>
        private static void ShiftLeft(ref BigInteger number, uint shift)
        {
            var powerOfTwo = BigInteger.Pow(s_bigTwo, (int)shift);
            number = number * powerOfTwo;
        }

        /// <summary>
        /// Multiply a BigInteger by the given power of ten.
        /// </summary>
        /// <param name="number">The BigInteger to multiply by a power of ten and replace with the product</param>
        /// <param name="power">The power of ten to multiply it by</param>
        private static void MultiplyByPowerOfTen(ref BigInteger number, uint power)
        {
            var powerOfTen = BigInteger.Pow(s_bigTen, (int)power);
            number = number * powerOfTen;
        }

        /// <summary>
        /// Convert a float value to the bits of its representation
        /// </summary>
        private static uint FloatToInt32Bits(float f)
        {
            var bits = default(FloatUnion);
            bits.FloatData = f;
            return bits.IntData;
        }

        /// <summary>
        /// Convert the bits of its representation to a float
        /// </summary>
        private static float Int32BitsToFloat(uint i)
        {
            var bits = default(FloatUnion);
            bits.IntData = i;
            return bits.FloatData;
        }

        /// <summary>
        /// A union used to convert between a float and the bits of its representation
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct FloatUnion
        {
            [FieldOffset(0)]
            public uint IntData;
            [FieldOffset(0)]
            public float FloatData;
        }
    }
}
