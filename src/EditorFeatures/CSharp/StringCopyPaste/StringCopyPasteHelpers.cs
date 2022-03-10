// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using MessagePack;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    internal static class StringCopyPasteHelpers
    {
        public static bool AllWhitespace(INormalizedTextChangeCollection changes)
        {
            foreach (var change in changes)
            {
                if (!AllWhitespace(change.NewText))
                    return false;
            }

            return true;
        }

        public static bool AllWhitespace(string text)
        {
            foreach (var ch in text)
            {
                if (!SyntaxFacts.IsWhitespace(ch))
                    return false;
            }

            return true;
        }

        public static bool ContainsControlCharacter(INormalizedTextChangeCollection changes)
        {
            return changes.Any(c => ContainsControlCharacter(c.NewText));
        }

        public static bool ContainsControlCharacter(string newText)
        {
            foreach (var c in newText)
            {
                if (char.IsControl(c))
                    return true;
            }

            return false;
        }

        public static string TrimStart(string value)
        {
            var start = 0;
            while (start < value.Length && SyntaxFacts.IsWhitespace(value[start]))
                start++;

            return value[start..];
        }

        public static bool IsRawStringLiteral(InterpolatedStringExpressionSyntax interpolatedString)
            => interpolatedString.StringStartToken.Kind() is SyntaxKind.InterpolatedSingleLineRawStringStartToken or SyntaxKind.InterpolatedMultiLineRawStringStartToken;

        public static bool IsRawStringLiteral(LiteralExpressionSyntax literal)
            => literal.Token.Kind() is SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken;

        public static TextSpan GetRawStringLiteralContentSpan(SourceText text, LiteralExpressionSyntax stringExpression)
            => GetRawStringLiteralContentSpan(text, stringExpression, out _);

        public static TextSpan GetRawStringLiteralContentSpan(
            SourceText text, LiteralExpressionSyntax stringExpression, out int delimiterQuoteCount)
        {
            Contract.ThrowIfFalse(IsRawStringLiteral(stringExpression));

            var start = stringExpression.SpanStart;
            while (start < text.Length && text[start] == '"')
                start++;

            delimiterQuoteCount = start - stringExpression.SpanStart;
            var end = stringExpression.Span.End;
            while (end > start && text[end - 1] == '"')
                end--;

            var contentSpan = TextSpan.FromBounds(start, end);
            return contentSpan;
        }

        public static int GetLongestQuoteSequence(SnapshotSpan contentSpan)
        {
            var snapshot = contentSpan.Snapshot;
            var longestCount = 0;
            for (int i = contentSpan.Start.Position, n = contentSpan.End.Position; i < n;)
            {
                if (snapshot[i] == '"')
                {
                    var j = i;
                    while (j < n && snapshot[j] == '"')
                        j++;

                    longestCount = Math.Max(longestCount, j - i);
                    i = j;
                }
                else
                {
                    i++;
                }
            }

            return longestCount;
        }
    }
}
