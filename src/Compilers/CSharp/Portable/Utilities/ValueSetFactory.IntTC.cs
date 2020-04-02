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
        private struct IntTC : INumericTC<int>
        {
            int INumericTC<int>.MinValue => int.MinValue;

            int INumericTC<int>.MaxValue => int.MaxValue;

            bool INumericTC<int>.Related(BinaryOperatorKind relation, int left, int right)
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

            int INumericTC<int>.FromConstantValue(ConstantValue constantValue) => constantValue.Int32Value;

            string INumericTC<int>.ToString(int value) => value.ToString();

            int INumericTC<int>.Random(Random random)
            {
                return (random.Next() << 10) ^ random.Next();
            }
        }
    }
}
