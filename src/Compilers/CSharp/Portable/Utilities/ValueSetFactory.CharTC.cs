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
        private struct CharTC : NumericTC<char>
        {
            char NumericTC<char>.MinValue => char.MinValue;

            char NumericTC<char>.MaxValue => char.MaxValue;

            (char leftMax, char rightMin) NumericTC<char>.Partition(char min, char max)
            {
                Debug.Assert(min < max);
                int half = (max - min) / 2;
                char leftMax = (char)(min + half);
                char rightMin = (char)(leftMax + 1);
                return (leftMax, rightMin);
            }

            bool NumericTC<char>.Related(BinaryOperatorKind relation, char left, char right)
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

            char NumericTC<char>.Next(char value)
            {
                Debug.Assert(value != char.MaxValue);
                return (char)(value + 1);
            }

            char EqualableValueTC<char>.FromConstantValue(ConstantValue constantValue) => constantValue.CharValue;

            string NumericTC<char>.ToString(char value) => $"'{value}'";
        }
    }
}
