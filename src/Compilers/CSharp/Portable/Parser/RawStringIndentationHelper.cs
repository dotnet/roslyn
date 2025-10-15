// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

/// <summary>
/// Helper methods for raw string literal indentation validation, shared between lexer and parser.
/// </summary>
internal static class RawStringIndentationHelper
{
    public interface IStringHelper<TString>
    {
        int GetLength(TString str);
        char GetCharAt(TString str, int index);

        string ToString(TString str);
        void AppendTo(TString str, StringBuilder sb);

        TString Slice(TString str, Range range);

        bool StartsWith(TString str, TString other);
    }

    public readonly struct StringAndSpanCharHelper : IStringHelper<(string str, TextSpan span)>
    {
        public int GetLength((string str, TextSpan span) tuple)
            => tuple.span.Length;

        public char GetCharAt((string str, TextSpan span) tuple, int index)
            => tuple.str[tuple.span.Start + index];

        public string ToString((string str, TextSpan span) tuple)
            => tuple.str.Substring(tuple.span.Start, tuple.span.Length);

        public void AppendTo((string str, TextSpan span) tuple, StringBuilder sb)
            => sb.Append(tuple.str, tuple.span.Start, tuple.span.Length);

        public (string str, TextSpan span) Slice((string str, TextSpan span) tuple, Range range)
        {
            var (offset, length) = range.GetOffsetAndLength(tuple.span.Length);
            return (tuple.str, new TextSpan(tuple.span.Start + offset, length));
        }

        public bool StartsWith((string str, TextSpan span) tuple, (string str, TextSpan span) other)
        {
            var strSpan = tuple.str.AsSpan(tuple.span.Start, tuple.span.Length);
            var otherSpan = other.str.AsSpan(other.span.Start, other.span.Length);

            return strSpan.StartsWith(otherSpan);
        }
    }

    public readonly struct StringBuilderCharHelper : IStringHelper<StringBuilder>
    {
        public int GetLength(StringBuilder str) => str.Length;
        public char GetCharAt(StringBuilder str, int index) => str[index];

        public string ToString(StringBuilder str) => str.ToString();

        public void AppendTo(StringBuilder str, StringBuilder sb) => sb.Append(str);

        StringBuilder IStringHelper<StringBuilder>.Slice(StringBuilder str, Range range)
            => throw new NotImplementedException();

        public bool StartsWith(StringBuilder sb, StringBuilder value)
        {
            if (sb.Length < value.Length)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                if (sb[i] != value[i])
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Checks if two whitespace sequences differ at a specific character position where both
    /// characters are whitespace but different types (e.g., tab vs space).
    /// </summary>
    public static bool CheckForSpaceDifference<TString, TStringHelper>(
        TString currentLineWhitespace,
        TString indentationLineWhitespace,
        [NotNullWhen(true)] out string? currentLineMessage,
        [NotNullWhen(true)] out string? indentationLineMessage)
        where TStringHelper : struct, IStringHelper<TString>
    {
        var helper = default(TStringHelper);
        for (int i = 0, n = Math.Min(helper.GetLength(currentLineWhitespace), helper.GetLength(indentationLineWhitespace)); i < n; i++)
        {
            var currentLineChar = helper.GetCharAt(currentLineWhitespace, i);
            var indentationLineChar = helper.GetCharAt(indentationLineWhitespace, i);

            if (currentLineChar != indentationLineChar &&
                SyntaxFacts.IsWhitespace(currentLineChar) &&
                SyntaxFacts.IsWhitespace(indentationLineChar))
            {
                currentLineMessage = CharToString(currentLineChar);
                indentationLineMessage = CharToString(indentationLineChar);
                return true;
            }
        }

        currentLineMessage = null;
        indentationLineMessage = null;
        return false;
    }

    /// <summary>
    /// Converts a whitespace character to its string representation for error messages.
    /// </summary>
    public static string CharToString(char ch)
    {
        return ch switch
        {
            '\t' => @"\t",
            '\v' => @"\v",
            '\f' => @"\f",
            _ => @$"\u{(int)ch:x4}",
        };
    }
}
