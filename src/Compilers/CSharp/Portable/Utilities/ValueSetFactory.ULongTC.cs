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
        private struct ULongTC : NumericTC<ulong>
        {
            ulong NumericTC<ulong>.MinValue => ulong.MinValue;

            ulong NumericTC<ulong>.MaxValue => ulong.MaxValue;

            (ulong leftMax, ulong rightMin) NumericTC<ulong>.Partition(ulong min, ulong max)
            {
                Debug.Assert(min != max);
                ulong half = (max - min) / 2;
                ulong leftMax = min + half;
                return (leftMax, leftMax + 1);
            }

            bool NumericTC<ulong>.Related(BinaryOperatorKind relation, ulong left, ulong right)
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

            ulong NumericTC<ulong>.Next(ulong value)
            {
                Debug.Assert(value != ulong.MaxValue);
                return value + 1;
            }

            ulong EqualableValueTC<ulong>.FromConstantValue(ConstantValue constantValue) => constantValue.UInt64Value;

            string NumericTC<ulong>.ToString(ulong value) => value.ToString();
        }
    }
}
