// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private struct IntTC : NumericTC<int>
        {
            int NumericTC<int>.MinValue => int.MinValue;
            int NumericTC<int>.MaxValue => int.MaxValue;
            (int leftMax, int rightMin) NumericTC<int>.Partition(int min, int max)
            {
                if (min == int.MinValue && max == int.MaxValue)
                    return (-1, 0);

                Debug.Assert((min < 0) == (max < 0));
                Debug.Assert(min != max);
                int half = (max - min) / 2;
                int leftMax = min + half;
                return (leftMax, leftMax + 1);
            }
            bool NumericTC<int>.Related(BinaryOperatorKind relation, int left, int right) => relation switch
            {
                Equal => left == right,
                GreaterThanOrEqual => left >= right,
                GreaterThan => left > right,
                LessThanOrEqual => left <= right,
                LessThan => left < right,
                NotEqual => left != right,
                _ => throw new ArgumentException("relation")
            };
            int NumericTC<int>.Next(int value) => value + 1;
            int EqualableValueTC<int>.FromConstantValue(ConstantValue constantValue) => constantValue.Int32Value;
        }
    }
}
