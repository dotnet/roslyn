// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private struct ShortTC : NumericTC<short>
        {
            short NumericTC<short>.MinValue => short.MinValue;
            short NumericTC<short>.MaxValue => short.MaxValue;
            (short leftMax, short rightMin) NumericTC<short>.Partition(short min, short max)
            {
                int half = (max - min) / 2;
                short leftMax = (short)(min + half);
                short rightMin = (short)(leftMax + 1);
                return (leftMax, rightMin);
            }
            bool NumericTC<short>.Related(BinaryOperatorKind relation, short left, short right) => relation switch
            {
                Equal => left == right,
                GreaterThanOrEqual => left >= right,
                GreaterThan => left > right,
                LessThanOrEqual => left <= right,
                LessThan => left < right,
                _ => throw new ArgumentException("relation")
            };
            short NumericTC<short>.Next(short value) => (short)(value + 1);
            short EqualableValueTC<short>.FromConstantValue(ConstantValue constantValue) => constantValue.Int16Value;
        }
    }
}
