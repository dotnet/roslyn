// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private struct UShortTC : NumericTC<ushort>
        {
            ushort NumericTC<ushort>.MinValue => ushort.MinValue;
            ushort NumericTC<ushort>.MaxValue => ushort.MaxValue;
            (ushort leftMax, ushort rightMin) NumericTC<ushort>.Partition(ushort min, ushort max)
            {
                int half = (max - min) / 2;
                ushort leftMax = (ushort)(min + half);
                ushort rightMin = (ushort)(leftMax + 1);
                return (leftMax, rightMin);
            }
            bool NumericTC<ushort>.Related(BinaryOperatorKind relation, ushort left, ushort right) => relation switch
            {
                Equal => left == right,
                GreaterThanOrEqual => left >= right,
                GreaterThan => left > right,
                LessThanOrEqual => left <= right,
                LessThan => left < right,
                _ => throw new ArgumentException("relation")
            };
            ushort NumericTC<ushort>.Next(ushort value) => (ushort)(value + 1);
            ushort EqualableValueTC<ushort>.FromConstantValue(ConstantValue constantValue) => constantValue.UInt16Value;
        }
    }
}
