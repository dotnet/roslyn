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
        private struct ULongTC : INumericTC<ulong>
        {
            ulong INumericTC<ulong>.MinValue => ulong.MinValue;

            ulong INumericTC<ulong>.MaxValue => ulong.MaxValue;

            (ulong leftMax, ulong rightMin) INumericTC<ulong>.Partition(ulong min, ulong max)
            {
                Debug.Assert(min < max);
                ulong half = (max - min) / 2;
                ulong leftMax = min + half;
                return (leftMax, leftMax + 1);
            }

            bool INumericTC<ulong>.Related(BinaryOperatorKind relation, ulong left, ulong right)
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

            ulong INumericTC<ulong>.Next(ulong value)
            {
                Debug.Assert(value != ulong.MaxValue);
                return value + 1;
            }

            ulong INumericTC<ulong>.FromConstantValue(ConstantValue constantValue) => constantValue.UInt64Value;

            string INumericTC<ulong>.ToString(ulong value) => value.ToString();
        }
    }
}
