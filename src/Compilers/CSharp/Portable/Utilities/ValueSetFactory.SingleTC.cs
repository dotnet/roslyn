// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private struct SingleTC : FloatingTC<float>, NumericTC<float>
        {
            float NumericTC<float>.MinValue => float.MinValue;

            float NumericTC<float>.MaxValue => float.MaxValue;

            float FloatingTC<float>.NaN => float.NaN;

            float FloatingTC<float>.MinusInf => float.NegativeInfinity;

            float FloatingTC<float>.PlusInf => float.PositiveInfinity;

            float NumericTC<float>.Next(float value)
            {
                Debug.Assert(!float.IsNaN(value));
                Debug.Assert(!float.IsInfinity(value));
                Debug.Assert(value != float.MaxValue);
                if (value < 0)
                {
                    if (value == -float.Epsilon)
                        return 0.0f; // skip negative zero
                    return -UintAsFloat(FloatAsUint(-value) - 1);
                }
                return UintAsFloat(FloatAsUint(value) + 1);
            }

            private unsafe static uint FloatAsUint(float d)
            {
                float* dp = &d;
                uint* lp = (uint*)dp;
                return *lp;
            }

            private unsafe static float UintAsFloat(uint l)
            {
                uint* lp = &l;
                float* dp = (float*)lp;
                return *dp;
            }

            (float leftMax, float rightMin) NumericTC<float>.Partition(float min, float max)
            {
                Debug.Assert(min != max);

                if (min == float.MinValue && max == float.MaxValue)
                    return (-UintAsFloat(1), 0.0f); // skip negative zero

                Debug.Assert((min >= 0) == (max >= 0));
                if (min < 0)
                {
                    uint minl = FloatAsUint(-max);
                    uint maxl = FloatAsUint(-min);
                    uint midl = minl + (maxl - minl) / 2;
                    return (-UintAsFloat(midl + 1), -UintAsFloat(midl));
                }
                else
                {
                    uint minl = FloatAsUint(min);
                    uint maxl = FloatAsUint(max);
                    uint midl = minl + (maxl - minl) / 2;
                    return (UintAsFloat(midl), UintAsFloat(midl + 1));
                }
            }

            bool NumericTC<float>.Related(BinaryOperatorKind relation, float left, float right)
            {
                switch (relation)
                {
                    case Equal:
                        return left == right || float.IsNaN(left) && float.IsNaN(right); // for our purposes, NaNs are equal
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

            float EqualableValueTC<float>.FromConstantValue(ConstantValue constantValue) => constantValue.SingleValue;

            string NumericTC<float>.ToString(float value) => FormattableString.Invariant($"{value:G9}");
        }
    }
}
