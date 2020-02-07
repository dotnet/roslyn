// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private struct ByteTC : NumericTC<byte>
        {
            byte NumericTC<byte>.MinValue => byte.MinValue;

            byte NumericTC<byte>.MaxValue => byte.MaxValue;

            (byte leftMax, byte rightMin) NumericTC<byte>.Partition(byte min, byte max)
            {
                Debug.Assert(min != max);
                int half = (max - min) / 2;
                byte leftMax = (byte)(min + half);
                byte rightMin = (byte)(leftMax + 1);
                return (leftMax, rightMin);
            }

            bool NumericTC<byte>.Related(BinaryOperatorKind relation, byte left, byte right)
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

            byte NumericTC<byte>.Next(byte value)
            {
                Debug.Assert(value != byte.MaxValue);
                return (byte)(value + 1);
            }

            byte EqualableValueTC<byte>.FromConstantValue(ConstantValue constantValue) => constantValue.ByteValue;
        }
    }
}
