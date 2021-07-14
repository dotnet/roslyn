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
        private struct IntTC : INumericTC<int>
        {
            int INumericTC<int>.MinValue => int.MinValue;

            int INumericTC<int>.MaxValue => int.MaxValue;

            int INumericTC<int>.Zero => 0;

            public bool Related(BinaryOperatorKind relation, int left, int right)
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

            int INumericTC<int>.Next(int value)
            {
                Debug.Assert(value != int.MaxValue);
                return value + 1;
            }

            int INumericTC<int>.Prev(int value)
            {
                Debug.Assert(value != int.MinValue);
                return value - 1;
            }

            public int FromConstantValue(ConstantValue constantValue) => constantValue.IsBad ? 0 : constantValue.Int32Value;

            public ConstantValue ToConstantValue(int value) => ConstantValue.Create(value);

            string INumericTC<int>.ToString(int value) => value.ToString();

            public int Random(Random random)
            {
                return (random.Next() << 10) ^ random.Next();
            }
        }
    }
}
