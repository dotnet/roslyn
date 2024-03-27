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
        private class LongTC : INumericTC<long>
        {
            public static readonly LongTC Instance = new LongTC();

            long INumericTC<long>.MinValue => long.MinValue;

            long INumericTC<long>.MaxValue => long.MaxValue;

            long INumericTC<long>.Zero => 0;

            bool INumericTC<long>.Related(BinaryOperatorKind relation, long left, long right)
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

            long INumericTC<long>.Next(long value)
            {
                Debug.Assert(value != long.MaxValue);
                return value + 1;
            }

            long INumericTC<long>.Prev(long value)
            {
                Debug.Assert(value != long.MinValue);
                return value - 1;
            }

            long INumericTC<long>.FromConstantValue(ConstantValue constantValue) => constantValue.IsBad ? 0L : constantValue.Int64Value;

            ConstantValue INumericTC<long>.ToConstantValue(long value) => ConstantValue.Create(value);

            string INumericTC<long>.ToString(long value) => value.ToString();

            long INumericTC<long>.Random(Random random)
            {
                return ((long)random.Next() << 35) ^ ((long)random.Next() << 10) ^ (long)random.Next();
            }
        }
    }
}
