// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    /// <summary>
    /// Helper methods for raw string literal indentation validation, shared between lexer and parser.
    /// </summary>
    internal static class RawStringIndentationHelper
    {
        /// <summary>
        /// Checks if two whitespace sequences differ at a specific character position where both
        /// characters are whitespace but different types (e.g., tab vs space).
        /// Works with StringBuilder for lexer use.
        /// </summary>
        public static bool CheckForSpaceDifference(
            StringBuilder currentLineWhitespace,
            StringBuilder indentationLineWhitespace,
            [NotNullWhen(true)] out string? currentLineMessage,
            [NotNullWhen(true)] out string? indentationLineMessage)
        {
            for (int i = 0, n = Math.Min(currentLineWhitespace.Length, indentationLineWhitespace.Length); i < n; i++)
            {
                var currentLineChar = currentLineWhitespace[i];
                var indentationLineChar = indentationLineWhitespace[i];

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
        /// Checks if two whitespace sequences differ at a specific character position where both
        /// characters are whitespace but different types (e.g., tab vs space).
        /// Works with ReadOnlySpan&lt;char&gt; for parser use.
        /// </summary>
        public static bool CheckForSpaceDifference(
            ReadOnlySpan<char> currentLineWhitespace,
            ReadOnlySpan<char> indentationLineWhitespace,
            [NotNullWhen(true)] out string? currentLineMessage,
            [NotNullWhen(true)] out string? indentationLineMessage)
        {
            for (int i = 0, n = Math.Min(currentLineWhitespace.Length, indentationLineWhitespace.Length); i < n; i++)
            {
                var currentLineChar = currentLineWhitespace[i];
                var indentationLineChar = indentationLineWhitespace[i];

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

        /// <summary>
        /// Returns true if <paramref name="sb"/> starts with <paramref name="value"/>.
        /// </summary>
        public static bool StartsWith(StringBuilder sb, StringBuilder value)
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
}
