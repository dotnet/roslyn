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
        private struct ULongTC : INumericTC<ulong>
        {
            readonly ulong INumericTC<ulong>.MinValue => ulong.MinValue;

            readonly ulong INumericTC<ulong>.MaxValue => ulong.MaxValue;

            readonly ulong INumericTC<ulong>.Zero => 0;

            readonly bool INumericTC<ulong>.Related(BinaryOperatorKind relation, ulong left, ulong right)
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

            readonly ulong INumericTC<ulong>.Next(ulong value)
            {
                Debug.Assert(value != ulong.MaxValue);
                return value + 1;
            }

            readonly ulong INumericTC<ulong>.Prev(ulong value)
            {
                Debug.Assert(value != ulong.MinValue);
                return value - 1;
            }

            readonly ulong INumericTC<ulong>.FromConstantValue(ConstantValue constantValue) => constantValue.IsBad ? 0UL : constantValue.UInt64Value;

            readonly ConstantValue INumericTC<ulong>.ToConstantValue(ulong value) => ConstantValue.Create(value);

            readonly string INumericTC<ulong>.ToString(ulong value) => value.ToString();

            readonly ulong INumericTC<ulong>.Random(Random random)
            {
                return ((ulong)random.Next() << 35) ^ ((ulong)random.Next() << 10) ^ (ulong)random.Next();
            }
        }
    }
}
