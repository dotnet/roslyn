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
        private struct SByteTC : INumericTC<sbyte>
        {
            sbyte INumericTC<sbyte>.MinValue => sbyte.MinValue;

            sbyte INumericTC<sbyte>.MaxValue => sbyte.MaxValue;

            sbyte INumericTC<sbyte>.Zero => 0;

            bool INumericTC<sbyte>.Related(BinaryOperatorKind relation, sbyte left, sbyte right)
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

            sbyte INumericTC<sbyte>.Next(sbyte value)
            {
                Debug.Assert(value != sbyte.MaxValue);
                return (sbyte)(value + 1);
            }

            sbyte INumericTC<sbyte>.Prev(sbyte value)
            {
                Debug.Assert(value != sbyte.MinValue);
                return (sbyte)(value - 1);
            }

            sbyte INumericTC<sbyte>.FromConstantValue(ConstantValue constantValue) => constantValue.IsBad ? (sbyte)0 : constantValue.SByteValue;

            public ConstantValue ToConstantValue(sbyte value) => ConstantValue.Create(value);

            string INumericTC<sbyte>.ToString(sbyte value) => value.ToString();

            sbyte INumericTC<sbyte>.Random(Random random)
            {
                return (sbyte)random.Next();
            }
        }
    }
}
