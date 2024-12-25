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
        private class ByteTC : INumericTC<byte>
        {
            public static readonly ByteTC Instance = new ByteTC();

            byte INumericTC<byte>.MinValue => byte.MinValue;

            byte INumericTC<byte>.MaxValue => byte.MaxValue;

            byte INumericTC<byte>.Zero => 0;

            bool INumericTC<byte>.Related(BinaryOperatorKind relation, byte left, byte right)
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

            byte INumericTC<byte>.Next(byte value)
            {
                Debug.Assert(value != byte.MaxValue);
                return (byte)(value + 1);
            }

            byte INumericTC<byte>.Prev(byte value)
            {
                Debug.Assert(value != byte.MinValue);
                return (byte)(value - 1);
            }

            byte INumericTC<byte>.FromConstantValue(ConstantValue constantValue) => constantValue.IsBad ? (byte)0 : constantValue.ByteValue;

            ConstantValue INumericTC<byte>.ToConstantValue(byte value) => ConstantValue.Create(value);

            string INumericTC<byte>.ToString(byte value) => value.ToString();

            byte INumericTC<byte>.Random(Random random)
            {
                return (byte)random.Next(byte.MinValue, byte.MaxValue + 1);
            }
        }
    }
}
