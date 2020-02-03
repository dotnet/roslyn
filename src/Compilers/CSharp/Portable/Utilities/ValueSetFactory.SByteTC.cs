// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private struct SByteTC : NumericTC<sbyte>
        {
            sbyte NumericTC<sbyte>.MinValue => sbyte.MinValue;
            sbyte NumericTC<sbyte>.MaxValue => sbyte.MaxValue;
            (sbyte leftMax, sbyte rightMin) NumericTC<sbyte>.Partition(sbyte min, sbyte max)
            {
                int half = (max - min) / 2;
                sbyte leftMax = (sbyte)(min + half);
                sbyte rightMin = (sbyte)(leftMax + 1);
                return (leftMax, rightMin);
            }
            bool NumericTC<sbyte>.Related(BinaryOperatorKind relation, sbyte left, sbyte right) => relation switch
            {
                Equal => left == right,
                GreaterThanOrEqual => left >= right,
                GreaterThan => left > right,
                LessThanOrEqual => left <= right,
                LessThan => left < right,
                NotEqual => left != right,
                _ => throw new ArgumentException("relation")
            };
            sbyte NumericTC<sbyte>.Next(sbyte value) => (sbyte)(value + 1);
            sbyte EqualableValueTC<sbyte>.FromConstantValue(ConstantValue constantValue) => constantValue.SByteValue;
        }
    }
}
