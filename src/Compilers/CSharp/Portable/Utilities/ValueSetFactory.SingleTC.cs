// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            /// <summary>
            /// The implementation of Next depends critically on the internal representation of an IEEE floating-point
            /// number.  Every bit sequence between the representation of 0 and MaxValue represents a distinct
            /// value, and the integer representations are ordered by value the same as the floating-point numbers they represent.
            /// </summary>
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

            /// <summary>
            /// The implementation of Partition depends critically on the internal representation of an IEEE floating-point
            /// number.  Every bit sequence between the representation of 0 and MaxValue represents a distinct
            /// value, and the integer representations are ordered by value the same as the floating-point numbers they represent.
            /// </summary>
            (float leftMax, float rightMin) NumericTC<float>.Partition(float min, float max)
            {
                Debug.Assert(min < max);

                if (min == float.MinValue && max == float.MaxValue)
                    return (-UintAsFloat(1), 0.0f); // skip negative zero

                Debug.Assert((min >= 0) == (max >= 0));

                // we partition the set of floating-point numbers in half.  Note that having the same
                // number of values on the left and the right (which is what we want) is not the same thing as the
                // numeric average of the two numbers (which would be a highly unbalanced partition)
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

            /// <summary>
            /// Produce a string for testing purposes that is likely to be the same independent of platform and locale.
            /// </summary>
            string NumericTC<float>.ToString(float value) => FormattableString.Invariant($"{value:G9}");
        }
    }
}
