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
            double INumericTC<double>.Next(double value)
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
                return (ulong)BitConverter.DoubleToInt64Bits(d);
            }

            private static double ULongAsDouble(ulong l)
            {
                return BitConverter.Int64BitsToDouble((long)l);
            }

            /// <summary>
            /// The implementation of Partition depends critically on the internal representation of an IEEE floating-point
            /// number.  Every bit sequence between the representation of 0 and MaxValue represents a distinct
            /// value, and the integer representations are ordered by value the same as the floating-point numbers they represent.
            /// </summary>
            (double leftMax, double rightMin) INumericTC<double>.Partition(double min, double max)
            {
                Debug.Assert(min < max);

                if (min == double.MinValue && max == double.MaxValue)
                    return (-ULongAsDouble(1), 0.0); // skip negative zero

                Debug.Assert((min >= 0) == (max >= 0));

                // we partition the set of floating-point numbers in half.  Note that having the same
                // number of values on the left and the right (which is what we want) is not the same thing as the
                // numeric average of the two numbers (which would be a highly unbalanced partition)
                if (min < 0)
                {
                    ulong minl = DoubleAsULong(-max);
                    ulong maxl = DoubleAsULong(-min);
                    ulong midl = minl + (maxl - minl) / 2;
                    return (-ULongAsDouble(midl + 1), -ULongAsDouble(midl));
                }
                else
                {
                    ulong minl = DoubleAsULong(min);
                    ulong maxl = DoubleAsULong(max);
                    ulong midl = minl + (maxl - minl) / 2;
                    return (ULongAsDouble(midl), ULongAsDouble(midl + 1));
                }
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
        }
    }
}
