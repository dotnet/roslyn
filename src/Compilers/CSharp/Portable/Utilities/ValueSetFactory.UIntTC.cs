// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private struct UIntTC : NumericTC<uint>
        {
            uint NumericTC<uint>.MinValue => uint.MinValue;
            uint NumericTC<uint>.MaxValue => uint.MaxValue;
            (uint leftMax, uint rightMin) NumericTC<uint>.Partition(uint min, uint max)
            {
                Debug.Assert(min != max);
                uint half = (max - min) / 2;
                uint leftMax = min + half;
                return (leftMax, leftMax + 1);
            }
            bool NumericTC<uint>.Related(BinaryOperatorKind relation, uint left, uint right) => relation switch
            {
                Equal => left == right,
                GreaterThanOrEqual => left >= right,
                GreaterThan => left > right,
                LessThanOrEqual => left <= right,
                LessThan => left < right,
                _ => throw new ArgumentException("relation")
            };
            uint NumericTC<uint>.Next(uint value) => value + 1;
            uint EqualableValueTC<uint>.FromConstantValue(ConstantValue constantValue) => constantValue.UInt32Value;
        }
    }
}
