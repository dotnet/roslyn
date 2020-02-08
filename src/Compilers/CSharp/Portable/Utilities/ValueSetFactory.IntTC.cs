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
                Debug.Assert(min != max);

                if (min == int.MinValue && max == int.MaxValue)
                    return (-1, 0);

                Debug.Assert((min < 0) == (max < 0));
                Debug.Assert(min != max);
                int half = (max - min) / 2;
                int leftMax = min + half;
                return (leftMax, leftMax + 1);
            }

            bool NumericTC<int>.Related(BinaryOperatorKind relation, int left, int right)
            {
                switch (relation)
                {
                    case Equal:
                        return left == right;
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

            int NumericTC<int>.Next(int value)
            {
                Debug.Assert(value != int.MaxValue);
                return value + 1;
            }

            int EqualableValueTC<int>.FromConstantValue(ConstantValue constantValue) => constantValue.Int32Value;

            string NumericTC<int>.ToString(int value) => value.ToString();
        }
    }
}
