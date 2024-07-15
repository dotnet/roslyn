// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private class SingleTC : FloatingTC<float>, INumericTC<float>
        {
            public static readonly SingleTC Instance = new SingleTC();

            float INumericTC<float>.MinValue => float.NegativeInfinity;

            float INumericTC<float>.MaxValue => float.PositiveInfinity;

            float FloatingTC<float>.NaN => float.NaN;

            float INumericTC<float>.Zero => 0;

            /// <summary>
            /// The implementation of Next depends critically on the internal representation of an IEEE floating-point
            /// number.  Every bit sequence between the representation of 0 and MaxValue represents a distinct
            /// value, and the integer representations are ordered by value the same as the floating-point numbers they represent.
            /// </summary>
            public float Next(float value)
            {
                Debug.Assert(!float.IsNaN(value));
                Debug.Assert(value != float.PositiveInfinity);

                if (value == 0)
                    return float.Epsilon;
                if (value < 0)
                {
                    if (value == -float.Epsilon)
                        return 0.0f; // skip negative zero
                    if (value == float.NegativeInfinity)
                        return float.MinValue;
                    return -UintAsFloat(FloatAsUint(-value) - 1);
                }
                if (value == float.MaxValue)
                    return float.PositiveInfinity;

                return UintAsFloat(FloatAsUint(value) + 1);
            }

            private static unsafe uint FloatAsUint(float d)
            {
                if (d == 0)
                    return 0;
                float* dp = &d;
                uint* lp = (uint*)dp;
                return *lp;
            }

            private static unsafe float UintAsFloat(uint l)
            {
                uint* lp = &l;
                float* dp = (float*)lp;
                return *dp;
            }

            bool INumericTC<float>.Related(BinaryOperatorKind relation, float left, float right)
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

            float INumericTC<float>.FromConstantValue(ConstantValue constantValue) => constantValue.IsBad ? 0.0F : constantValue.SingleValue;

            ConstantValue INumericTC<float>.ToConstantValue(float value) => ConstantValue.Create(value);

            /// <summary>
            /// Produce a string for testing purposes that is likely to be the same independent of platform and locale.
            /// </summary>
            string INumericTC<float>.ToString(float value) =>
                float.IsNaN(value) ? "NaN" :
                value == float.NegativeInfinity ? "-Inf" :
                value == float.PositiveInfinity ? "Inf" :
                FormattableString.Invariant($"{value:G9}");

            float INumericTC<float>.Prev(float value)
            {
                return -Next(-value);
            }

            float INumericTC<float>.Random(Random random)
            {
                return (float)(random.NextDouble() * 100 - 50);
            }
        }
    }
}
