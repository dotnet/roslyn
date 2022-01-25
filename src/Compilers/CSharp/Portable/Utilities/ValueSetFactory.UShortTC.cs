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
        private struct UShortTC : INumericTC<ushort>
        {
            ushort INumericTC<ushort>.MinValue => ushort.MinValue;

            ushort INumericTC<ushort>.MaxValue => ushort.MaxValue;

            ushort INumericTC<ushort>.Zero => 0;

            bool INumericTC<ushort>.Related(BinaryOperatorKind relation, ushort left, ushort right)
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

            ushort INumericTC<ushort>.Next(ushort value)
            {
                Debug.Assert(value != ushort.MaxValue);
                return (ushort)(value + 1);
            }

            ushort INumericTC<ushort>.FromConstantValue(ConstantValue constantValue) => constantValue.IsBad ? (ushort)0 : constantValue.UInt16Value;

            ConstantValue INumericTC<ushort>.ToConstantValue(ushort value) => ConstantValue.Create(value);

            string INumericTC<ushort>.ToString(ushort value) => value.ToString();

            ushort INumericTC<ushort>.Prev(ushort value)
            {
                Debug.Assert(value != ushort.MinValue);
                return (ushort)(value - 1);
            }

            ushort INumericTC<ushort>.Random(Random random)
            {
                return (ushort)random.Next();
            }
        }
    }
}
