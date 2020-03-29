// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private struct DoubleTC : FloatingTC<double>, INumericTC<double>
        {
            double INumericTC<double>.MinValue => double.MinValue;

            double INumericTC<double>.MaxValue => double.MaxValue;

            double FloatingTC<double>.NaN => double.NaN;

            double FloatingTC<double>.MinusInf => double.NegativeInfinity;

            double FloatingTC<double>.PlusInf => double.PositiveInfinity;

            /// <summary>
            /// The implementation of Next depends critically on the internal representation of an IEEE floating-point
            /// number.  Every bit sequence between the representation of 0 and MaxValue represents a distinct
            /// value, and the integer representations are ordered by value the same as the floating-point numbers they represent.
            /// </summary>
            public double Next(double value)
            {
                Debug.Assert(!double.IsNaN(value));
                Debug.Assert(!double.IsInfinity(value));
                Debug.Assert(value != double.MaxValue);
                if (value < 0)
                {
                    if (value == -double.Epsilon)
                        return 0.0; // skip negative zero
                    return -ULongAsDouble(DoubleAsULong(-value) - 1);
                }

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

            double INumericTC<double>.FromConstantValue(ConstantValue constantValue) => constantValue.DoubleValue;

            /// <summary>
            /// Produce a string for testing purposes that is likely to be the same independent of platform and locale.
            /// </summary>
            string INumericTC<double>.ToString(double value) => FormattableString.Invariant($"{value:G17}");

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
