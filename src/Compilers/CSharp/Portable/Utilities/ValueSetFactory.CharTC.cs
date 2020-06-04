﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private struct CharTC : INumericTC<char>
        {
            char INumericTC<char>.MinValue => char.MinValue;

            char INumericTC<char>.MaxValue => char.MaxValue;

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

            char INumericTC<char>.FromConstantValue(ConstantValue constantValue) => constantValue.IsBad ? (char)0 : constantValue.CharValue;

            string INumericTC<char>.ToString(char c)
            {
                var isPrintable = char.IsWhiteSpace(c) ||
                    // exclude the Unicode character categories containing non-rendering,
                    // unknown, or incomplete characters.
                    char.GetUnicodeCategory(c) switch { UnicodeCategory.Control => false, UnicodeCategory.OtherNotAssigned => false, UnicodeCategory.Surrogate => false, _ => true };
                return isPrintable ? $"'{c}'" : $"\\u{(int)c:X4}";
            }

            char INumericTC<char>.Prev(char value)
            {
                Debug.Assert(value != char.MinValue);
                return (char)(value - 1);
            }

            char INumericTC<char>.Random(Random random)
            {
                return (char)random.Next((int)char.MinValue, 1 + (int)char.MaxValue);
            }

            ConstantValue INumericTC<char>.ToConstantValue(char value) => ConstantValue.Create(value);
        }
    }
}
