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
        private struct NonNegativeIntTC : INumericTC<int>
        {
            readonly int INumericTC<int>.MinValue => 0;

            readonly int INumericTC<int>.MaxValue => int.MaxValue;

            readonly int INumericTC<int>.Zero => 0;

            public readonly bool Related(BinaryOperatorKind relation, int left, int right)
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

            readonly int INumericTC<int>.Next(int value)
            {
                Debug.Assert(value != int.MaxValue);
                return value + 1;
            }

            readonly int INumericTC<int>.Prev(int value)
            {
                Debug.Assert(value != 0);
                return value - 1;
            }

            public readonly int FromConstantValue(ConstantValue constantValue)
            {
                // We could have a negate value in source, but it won't get past NonNegativeIntValueSetFactory.Related
                return constantValue.IsBad ? 0 : constantValue.Int32Value;
            }

            public readonly ConstantValue ToConstantValue(int value)
            {
                return ConstantValue.Create(value);
            }

            readonly string INumericTC<int>.ToString(int value)
            {
                return value.ToString();
            }

            public readonly int Random(Random random)
            {
                return Math.Abs((random.Next() << 10) ^ random.Next());
            }
        }
    }
}
