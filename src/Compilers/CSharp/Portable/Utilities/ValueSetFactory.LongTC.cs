// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private struct LongTC : NumericTC<long>
        {
            long NumericTC<long>.MinValue => long.MinValue;
            long NumericTC<long>.MaxValue => long.MaxValue;
            (long leftMax, long rightMin) NumericTC<long>.Partition(long min, long max)
            {
                if (min == long.MinValue && max == long.MaxValue)
                    return (-1, 0);

                Debug.Assert((min < 0) == (max < 0));
                Debug.Assert(min != max);
                long half = (max - min) / 2;
                long leftMax = min + half;
                return (leftMax, leftMax + 1);
            }
            bool NumericTC<long>.Related(BinaryOperatorKind relation, long left, long right) => relation switch
            {
                Equal => left == right,
                GreaterThanOrEqual => left >= right,
                GreaterThan => left > right,
                LessThanOrEqual => left <= right,
                LessThan => left < right,
                NotEqual => left != right,
                _ => throw new ArgumentException("relation")
            };
            long NumericTC<long>.Next(long value) => value + 1;
            long EqualableValueTC<long>.FromConstantValue(ConstantValue constantValue) => constantValue.Int64Value;
        }
    }
}
