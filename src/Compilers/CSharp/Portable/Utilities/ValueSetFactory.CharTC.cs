// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            readonly char INumericTC<char>.MinValue => char.MinValue;

            readonly char INumericTC<char>.MaxValue => char.MaxValue;

            readonly char INumericTC<char>.Zero => (char)0;

            readonly bool INumericTC<char>.Related(BinaryOperatorKind relation, char left, char right)
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

            readonly char INumericTC<char>.Next(char value)
            {
                Debug.Assert(value != char.MaxValue);
                return (char)(value + 1);
            }

            readonly char INumericTC<char>.FromConstantValue(ConstantValue constantValue) => constantValue.IsBad ? (char)0 : constantValue.CharValue;

            readonly string INumericTC<char>.ToString(char c)
            {
                return ObjectDisplay.FormatPrimitive(c, ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.UseQuotes);
            }

            readonly char INumericTC<char>.Prev(char value)
            {
                Debug.Assert(value != char.MinValue);
                return (char)(value - 1);
            }

            readonly char INumericTC<char>.Random(Random random)
            {
                return (char)random.Next((int)char.MinValue, 1 + (int)char.MaxValue);
            }

            readonly ConstantValue INumericTC<char>.ToConstantValue(char value) => ConstantValue.Create(value);
        }
    }
}
