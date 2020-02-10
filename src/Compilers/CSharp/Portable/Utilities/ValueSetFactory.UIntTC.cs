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

            bool NumericTC<uint>.Related(BinaryOperatorKind relation, uint left, uint right)
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

            uint NumericTC<uint>.Next(uint value)
            {
                Debug.Assert(value != uint.MaxValue);
                return value + 1;
            }

            uint EqualableValueTC<uint>.FromConstantValue(ConstantValue constantValue) => constantValue.UInt32Value;

            string NumericTC<uint>.ToString(uint value) => value.ToString();
        }
    }
}
