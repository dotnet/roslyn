// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private class DoubleTC : FloatingTC<double>, INumericTC<double>
        {
            public static readonly DoubleTC Instance = new DoubleTC();

            double INumericTC<double>.MinValue => double.NegativeInfinity;

            double INumericTC<double>.MaxValue => double.PositiveInfinity;

            double FloatingTC<double>.NaN => double.NaN;

            double INumericTC<double>.Zero => 0.0;

            /// <summary>
            /// The implementation of Next depends critically on the internal representation of an IEEE floating-point
            /// number.  Every bit sequence between the representation of 0 and MaxValue represents a distinct
            /// value, and the integer representations are ordered by value the same as the floating-point numbers they represent.
            /// </summary>
            public double Next(double value)
            {
                Debug.Assert(!double.IsNaN(value));
                Debug.Assert(value != double.PositiveInfinity);

                if (value == 0)
                    return double.Epsilon;
                if (value < 0)
                {
                    if (value == -double.Epsilon)
                        return 0.0; // skip negative zero
                    if (value == double.NegativeInfinity)
                        return double.MinValue;
                    return -ULongAsDouble(DoubleAsULong(-value) - 1);
                }
                if (value == double.MaxValue)
                    return double.PositiveInfinity;

                return ULongAsDouble(DoubleAsULong(value) + 1);
            }

            private static ulong DoubleAsULong(double d)
            {
                if (d == 0)
                    return 0;
                return (ulong)BitConverter.DoubleToInt64Bits(d);
            }

            private static double ULongAsDouble(ulong l)
            {
                return BitConverter.Int64BitsToDouble((long)l);
            }

            bool INumericTC<double>.Related(BinaryOperatorKind relation, double left, double right)
            {
                switch (relation)
                {
                    case Equal:
                        return left == right || double.IsNaN(left) && double.IsNaN(right); // for our purposes, NaNs are equal
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

            double INumericTC<double>.FromConstantValue(ConstantValue constantValue) => constantValue.IsBad ? 0.0 : constantValue.DoubleValue;

            ConstantValue INumericTC<double>.ToConstantValue(double value) => ConstantValue.Create(value);

            /// <summary>
            /// Produce a string for testing purposes that is likely to be the same independent of platform and locale.
            /// </summary>
            string INumericTC<double>.ToString(double value) =>
                double.IsNaN(value) ? "NaN" :
                value == double.NegativeInfinity ? "-Inf" :
                value == double.PositiveInfinity ? "Inf" :
                FormattableString.Invariant($"{value:G17}");

            double INumericTC<double>.Prev(double value)
            {
                return -Next(-value);
            }

            double INumericTC<double>.Random(Random random)
            {
                return random.NextDouble() * 100 - 50;
            }
        }
    }
}
