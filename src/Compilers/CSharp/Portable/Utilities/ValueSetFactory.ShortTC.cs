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
        private struct ShortTC : NumericTC<short>
        {
            short NumericTC<short>.MinValue => short.MinValue;

            short NumericTC<short>.MaxValue => short.MaxValue;

            (short leftMax, short rightMin) NumericTC<short>.Partition(short min, short max)
            {
                Debug.Assert(min < max);
                int half = (max - min) / 2;
                short leftMax = (short)(min + half);
                short rightMin = (short)(leftMax + 1);
                return (leftMax, rightMin);
            }

            bool NumericTC<short>.Related(BinaryOperatorKind relation, short left, short right)
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

            short NumericTC<short>.Next(short value)
            {
                Debug.Assert(value != short.MaxValue);
                return (short)(value + 1);
            }

            short EqualableValueTC<short>.FromConstantValue(ConstantValue constantValue) => constantValue.Int16Value;

            string NumericTC<short>.ToString(short value) => value.ToString();
        }
    }
}
