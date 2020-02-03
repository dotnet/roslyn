// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private struct ByteTC : NumericTC<byte>
        {
            byte NumericTC<byte>.MinValue => byte.MinValue;
            byte NumericTC<byte>.MaxValue => byte.MaxValue;
            (byte leftMax, byte rightMin) NumericTC<byte>.Partition(byte min, byte max)
            {
                int half = (max - min) / 2;
                byte leftMax = (byte)(min + half);
                byte rightMin = (byte)(leftMax + 1);
                return (leftMax, rightMin);
            }
            bool NumericTC<byte>.Related(BinaryOperatorKind relation, byte left, byte right) => relation switch
            {
                Equal => left == right,
                GreaterThanOrEqual => left >= right,
                GreaterThan => left > right,
                LessThanOrEqual => left <= right,
                LessThan => left < right,
                NotEqual => left != right,
                _ => throw new ArgumentException("relation")
            };
            byte NumericTC<byte>.Next(byte value) => (byte)(value + 1);
            byte EqualableValueTC<byte>.FromConstantValue(ConstantValue constantValue) => constantValue.ByteValue;
        }
    }
}
