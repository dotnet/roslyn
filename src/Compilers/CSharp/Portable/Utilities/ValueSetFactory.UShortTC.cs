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
        private struct UShortTC : NumericTC<ushort>
        {
            ushort NumericTC<ushort>.MinValue => ushort.MinValue;

            ushort NumericTC<ushort>.MaxValue => ushort.MaxValue;

            (ushort leftMax, ushort rightMin) NumericTC<ushort>.Partition(ushort min, ushort max)
            {
                Debug.Assert(min < max);
                int half = (max - min) / 2;
                ushort leftMax = (ushort)(min + half);
                ushort rightMin = (ushort)(leftMax + 1);
                return (leftMax, rightMin);
            }

            bool NumericTC<ushort>.Related(BinaryOperatorKind relation, ushort left, ushort right)
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

            ushort NumericTC<ushort>.Next(ushort value)
            {
                Debug.Assert(value != ushort.MaxValue);
                return (ushort)(value + 1);
            }

            ushort EqualableValueTC<ushort>.FromConstantValue(ConstantValue constantValue) => constantValue.UInt16Value;

            string NumericTC<ushort>.ToString(ushort value) => value.ToString();
        }
    }
}
