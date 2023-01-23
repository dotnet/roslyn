// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private struct DecimalTC : INumericTC<decimal>
        {
            // These are the smallest nonzero normal mantissa value (in three parts) below which you could use a higher scale.
            // This is the 96-bit representation of ((2^96)-1) / 10;
            private const uint transitionLow = 0x99999999;
            private const uint transitionMid = 0x99999999;
            private const uint transitionHigh = 0x19999999;
            private const byte maxScale = 28;

            private static readonly decimal normalZero = new decimal(lo: 0, mid: 0, hi: 0, isNegative: false, scale: maxScale);
            private static readonly decimal epsilon = new decimal(lo: 1, mid: 0, hi: 0, isNegative: false, scale: maxScale);

            decimal INumericTC<decimal>.MinValue => decimal.MinValue;

            decimal INumericTC<decimal>.MaxValue => decimal.MaxValue;

            decimal INumericTC<decimal>.Zero => 0M;

            public decimal FromConstantValue(ConstantValue constantValue) => constantValue.IsBad ? 0m : constantValue.DecimalValue;

            public ConstantValue ToConstantValue(decimal value) => ConstantValue.Create(value);

            public decimal Next(decimal value)
            {
                Debug.Assert(value != decimal.MaxValue);
                if (value == 0m)
                    return epsilon;
                var (low, mid, high, isNegative, scale) = DecimalRep.FromValue(value);
                Debug.Assert(scale == DecimalRep.FromValue(value).Normalize().scale); // assert that the input is normalized
                if (isNegative)
                {
                    // get the next value closer to zero.(less negative)
                    if (value == -epsilon)
                        return normalZero; // skip negative zero

                    // This should not occur, as numbers such as this are not in our normal form (not at maximum scale).
                    Debug.Assert(!(scale < 28 && low == transitionLow && mid == transitionMid && high == transitionHigh));

                    if (low != 0)
                        return new DecimalRep(low: low - 1, mid: mid, high: high, isNegative: isNegative, scale: scale).Value;
                    if (mid != 0)
                        return new DecimalRep(low: uint.MaxValue, mid: mid - 1, high: high, isNegative: isNegative, scale: scale).Value;
                    Debug.Assert(high > 0); // otherwise value == 0m
                    return new DecimalRep(low: uint.MaxValue, mid: uint.MaxValue, high: high - 1, isNegative: isNegative, scale: scale).Value;
                }
                else
                {
                    // get the next value farther from zero.(more positive)
                    if (low != uint.MaxValue)
                        return new DecimalRep(low: low + 1, mid: mid, high: high, isNegative: isNegative, scale: scale).Value;
                    if (mid != uint.MaxValue)
                        return new DecimalRep(low: 0, mid: mid + 1, high: high, isNegative: isNegative, scale: scale).Value;
                    if (high != uint.MaxValue)
                        return new DecimalRep(low: 0, mid: 0, high: high + 1, isNegative: isNegative, scale: scale).Value;

                    // the mantissa it at its maximum value.  Divide the mantissa by 10 and decrease the scale.
                    // Since we know the value of the mantissa, we can simply assign mantissa/10 here.
                    low = transitionLow;
                    mid = transitionMid;
                    high = transitionHigh;
                    Debug.Assert(scale > 0); // otherwise value == decimal.MaxValue
                    scale -= 1;

                    var result = new DecimalRep(low: low + 1, mid: mid, high: high, isNegative: isNegative, scale: scale).Value;

                    // Assert that the value returned really is the next possible value.
                    Debug.Assert(new DecimalRep(low: low, mid: mid, high: high, isNegative: isNegative, scale: scale).Value <= value);
                    Debug.Assert(result > value);
                    return result;
                }
            }

            bool INumericTC<decimal>.Related(BinaryOperatorKind relation, decimal left, decimal right)
            {
                switch (relation)
                {
                    case Equal:
                        return left == right;
                    case GreaterThanOrEqual:
                        return left >= right;
                    case GreaterThan:
                        return left > right;
                    case LessThanOrEqual:
                        return left <= right;
                    case LessThan:
                        return left < right;
                    default:
                        throw new ArgumentException("relation");
                }
            }

            string INumericTC<decimal>.ToString(decimal value) => FormattableString.Invariant($"{value:G}");

            decimal INumericTC<decimal>.Prev(decimal value)
            {
                return -Next(-value);
            }

            public decimal Random(Random random)
            {
                INumericTC<uint> uinttc = default(UIntTC);
                return new DecimalRep(
                    low: uinttc.Random(random),
                    mid: uinttc.Random(random),
                    high: uinttc.Random(random),
                    isNegative: random.NextDouble() < 0.5,
                    scale: (byte)random.Next(0, maxScale + 1)).Normalize().Value;
            }

            public static decimal Normalize(decimal value) => DecimalRep.FromValue(value).Normalize().Value;

            private readonly struct DecimalRep
            {
                public readonly uint low;
                public readonly uint mid;
                public readonly uint high;
                public readonly bool isNegative;
                public readonly byte scale;

                public DecimalRep(uint low, uint mid, uint high, bool isNegative, byte scale)
                {
                    if (scale > maxScale)
                        throw new ArgumentException("scale");

                    this.low = low;
                    this.mid = mid;
                    this.high = high;
                    this.isNegative = isNegative;
                    this.scale = scale;
                }

                public decimal Value => new decimal(lo: (int)low, mid: (int)mid, hi: (int)high, isNegative: isNegative, scale: scale);

                public DecimalRep Normalize()
                {
                    // return the number in the highest possible scale (containing the most precision)
                    if (this.scale == maxScale)
                        return this;

                    var (low, mid, high, isNegative, scale) = this;

                    while (scale < maxScale)
                    {
                        if (high * 10L > uint.MaxValue)
                            break;

                        long newHigh = 10L * high;
                        long newMid = 10L * mid;
                        long newLow = 10L * low;
                        newMid += newLow >> 32;
                        newLow &= uint.MaxValue;
                        newHigh += newMid >> 32;
                        newMid &= uint.MaxValue;
                        if (newHigh > uint.MaxValue)
                            break;

                        low = (uint)newLow;
                        mid = (uint)newMid;
                        high = (uint)newHigh;
                        scale += 1;
                    }

                    return new DecimalRep(low, mid, high, isNegative, scale);
                }

                public static DecimalRep FromValue(decimal value)
                {
                    value.GetBits(out bool isNegative, out byte scale, out uint low, out uint mid, out uint high);
                    Debug.Assert(scale <= maxScale);
                    return new DecimalRep(low: low, mid: mid, high: high, isNegative: isNegative, scale: scale);
                }

                public void Deconstruct(out uint low, out uint mid, out uint high, out bool isNegative, out byte scale) =>
                    (low, mid, high, isNegative, scale) = (this.low, this.mid, this.high, this.isNegative, this.scale);

                public override string ToString() => $"Decimal({(isNegative ? "-" : "+")}, 0x{high:08X} 0x{mid:08X} 0x{low:08X} *10^-{scale})";
            }
        }
    }
}
