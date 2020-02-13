// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private struct CharTC : INumericTC<char>
        {
            char INumericTC<char>.MinValue => char.MinValue;

            char INumericTC<char>.MaxValue => char.MaxValue;

            (char leftMax, char rightMin) INumericTC<char>.Partition(char min, char max)
            {
                Debug.Assert(min < max);
                int half = (max - min) / 2;
                char leftMax = (char)(min + half);
                char rightMin = (char)(leftMax + 1);
                return (leftMax, rightMin);
            }

            bool INumericTC<char>.Related(BinaryOperatorKind relation, char left, char right)
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

            char INumericTC<char>.Next(char value)
            {
                Debug.Assert(value != char.MaxValue);
                return (char)(value + 1);
            }

            char INumericTC<char>.FromConstantValue(ConstantValue constantValue) => constantValue.CharValue;

            string INumericTC<char>.ToString(char c)
            {
                // The set of Unicode character categories containing non-rendering,
                // unknown, or incomplete characters.
                var nonRenderingCategories = new UnicodeCategory[] {
                    UnicodeCategory.Control,
                    UnicodeCategory.OtherNotAssigned,
                    UnicodeCategory.Surrogate };
                var isPrintable = char.IsWhiteSpace(c) || !nonRenderingCategories.Contains(char.GetUnicodeCategory(c));
                return isPrintable ? $"'{c}'" : $"\\u{(int)c:X4}";
            }
        }
    }
}
