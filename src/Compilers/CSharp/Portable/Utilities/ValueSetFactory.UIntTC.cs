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
        private struct UIntTC : INumericTC<uint>
        {
            uint INumericTC<uint>.MinValue => uint.MinValue;

            uint INumericTC<uint>.MaxValue => uint.MaxValue;

            bool INumericTC<uint>.Related(BinaryOperatorKind relation, uint left, uint right)
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

            uint INumericTC<uint>.Next(uint value)
            {
                Debug.Assert(value != uint.MaxValue);
                return value + 1;
            }

            uint INumericTC<uint>.FromConstantValue(ConstantValue constantValue) => constantValue.UInt32Value;

            string INumericTC<uint>.ToString(uint value) => value.ToString();

            uint INumericTC<uint>.Prev(uint value)
            {
                Debug.Assert(value != uint.MinValue);
                return value - 1;
            }

            uint INumericTC<uint>.Random(Random random)
            {
                return (uint)((random.Next() << 10) ^ random.Next());
            }
        }
    }
}
