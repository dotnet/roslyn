// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private struct ULongTC : NumericTC<ulong>
        {
            ulong NumericTC<ulong>.MinValue => ulong.MinValue;
            ulong NumericTC<ulong>.MaxValue => ulong.MaxValue;
            (ulong leftMax, ulong rightMin) NumericTC<ulong>.Partition(ulong min, ulong max)
            {
                Debug.Assert(min != max);
                ulong half = (max - min) / 2;
                ulong leftMax = min + half;
                return (leftMax, leftMax + 1);
            }
            bool NumericTC<ulong>.Related(BinaryOperatorKind relation, ulong left, ulong right) => relation switch
            {
                Equal => left == right,
                GreaterThanOrEqual => left >= right,
                GreaterThan => left > right,
                LessThanOrEqual => left <= right,
                LessThan => left < right,
                _ => throw new ArgumentException("relation")
            };
            ulong NumericTC<ulong>.Next(ulong value) => value + 1;
            ulong EqualableValueTC<ulong>.FromConstantValue(ConstantValue constantValue) => constantValue.UInt64Value;
        }
    }
}
