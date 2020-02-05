// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private struct DoubleTC : FloatingTC<double>, NumericTC<double>
        {
            double NumericTC<double>.MinValue => double.MinValue;
            double NumericTC<double>.MaxValue => double.MaxValue;
            double FloatingTC<double>.NaN => double.NaN;
            double FloatingTC<double>.MinusInf => double.NegativeInfinity;
            double FloatingTC<double>.PlusInf => double.PositiveInfinity;

            double NumericTC<double>.Next(double value)
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
            private unsafe static ulong DoubleAsULong(double d)
            {
                double* dp = &d;
                ulong* lp = (ulong*)dp;
                return *lp;
            }
            private unsafe static double ULongAsDouble(ulong l)
            {
                ulong* lp = &l;
                double* dp = (double*)lp;
                return *dp;
            }

            (double leftMax, double rightMin) NumericTC<double>.Partition(double min, double max)
            {
                if (min == double.MinValue && max == double.MaxValue)
                    return (-ULongAsDouble(1), 0.0); // skip negative zero

                Debug.Assert((min >= 0) == (max >= 0));
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

            bool NumericTC<double>.Related(BinaryOperatorKind relation, double left, double right) => relation switch
            {
                Equal => left == right || double.IsNaN(left) && double.IsNaN(right), // pretend NaNs are equal
                GreaterThanOrEqual => left >= right,
                GreaterThan => left > right,
                LessThanOrEqual => left <= right,
                LessThan => left < right,
                _ => throw new ArgumentException("relation")
            };

            double EqualableValueTC<double>.FromConstantValue(ConstantValue constantValue) => constantValue.DoubleValue;

            string FloatingTC<double>.ToString(double value) => $"{value:G17}";
        }
    }
}
