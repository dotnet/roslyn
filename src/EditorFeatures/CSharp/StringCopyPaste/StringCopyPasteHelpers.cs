// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    internal static class StringCopyPasteHelpers
    {
        public static bool HasNewLine(TextLine line)
            => line.Span.End != line.SpanIncludingLineBreak.End;

        /// <summary>
        /// True if the string literal contains an error diagnostic that indicates a parsing problem with it. For
        /// interpolated strings, this only includes the text sections, and not any interpolation holes in the literal.
        /// </summary>
        public static bool ContainsError(ExpressionSyntax stringExpression)
        {
            if (stringExpression is LiteralExpressionSyntax)
                return NodeOrTokenContainsError(stringExpression);

            if (stringExpression is InterpolatedStringExpressionSyntax interpolatedString)
            {
                using var _ = PooledHashSet<Diagnostic>.GetInstance(out var errors);
                foreach (var diagnostic in interpolatedString.GetDiagnostics())
                {
                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                        errors.Add(diagnostic);
                }

                // we don't care about errors in holes.  Only errors in the content portions of the string.
                for (int i = 0, n = interpolatedString.Contents.Count; i < n && errors.Count > 0; i++)
                {
                    if (interpolatedString.Contents[i] is InterpolatedStringTextSyntax text)
                    {
                        foreach (var diagnostic in text.GetDiagnostics())
                            errors.Remove(diagnostic);
                    }
                }

                return errors.Count > 0;
            }

            throw ExceptionUtilities.UnexpectedValue(stringExpression);
        }

        public static bool NodeOrTokenContainsError(SyntaxNodeOrToken nodeOrToken)
        {
            foreach (var diagnostic in nodeOrToken.GetDiagnostics())
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                    return true;
            }

            return false;
        }

        public static bool AllWhitespace(INormalizedTextChangeCollection changes)
        {
            foreach (var change in changes)
            {
                if (!AllWhitespace(change.NewText))
                    return false;
            }

            return true;
        }

        private static bool AllWhitespace(string text)
        {
            foreach (var ch in text)
            {
                if (!SyntaxFacts.IsWhitespace(ch))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Given a TextLine, returns the index (in the SourceText) of the first character of it that is not a
        /// Whitespace character.  The LineBreak parts of the line are not considered here.  If the line is empty/blank
        /// (again, not counting LineBreak characters) then -1 is returned.
        /// </summary>
        public static int GetFirstNonWhitespaceIndex(SourceText text, TextLine line)
        {
            for (int i = line.Start, n = line.End; i < n; i++)
            {
                if (!SyntaxFacts.IsWhitespace(text[i]))
                    return i;
            }

            return -1;
        }

        public static bool ContainsControlCharacter(INormalizedTextChangeCollection changes)
            => changes.Any(c => ContainsControlCharacter(c.NewText));

        public static bool ContainsControlCharacter(string newText)
        {
            foreach (var c in newText)
            {
                if (char.IsControl(c))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Removes all characters matching <see cref="SyntaxFacts.IsWhitespace(char)"/> from the start of <paramref
        /// name="value"/>.
        /// </summary>
        public static (string whitespace, string contents) ExtractWhitespace(string value)
        {
            var start = 0;
            while (start < value.Length && SyntaxFacts.IsWhitespace(value[start]))
                start++;

            return (value[..start], value[start..]);
        }

        public static bool IsRawStringLiteral(InterpolatedStringExpressionSyntax interpolatedString)
            => interpolatedString.StringStartToken.Kind() is SyntaxKind.InterpolatedSingleLineRawStringStartToken or SyntaxKind.InterpolatedMultiLineRawStringStartToken;

        public static bool IsRawStringLiteral(LiteralExpressionSyntax literal)
            => literal.Token.Kind() is SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken;

        /// <summary>
        /// Given a string literal or interpolated string, returns the subspans of those expressions that are actual
        /// text content spans.  For a string literal, this is the span between the quotes.  For an interpolated string
        /// this is the text regions between the holes.  Note that for interpolated strings the content spans may be
        /// empty (for example, between two adjacent holes).  We still want to know about those empty spans so that if a
        /// paste happens into that empty region that we still escape properly.
        /// </summary>
        public static ImmutableArray<TextSpan> GetTextContentSpans(
            SourceText text, ExpressionSyntax stringExpression)
        {
            if (stringExpression is LiteralExpressionSyntax literal)
            {
                // simple string literal (normal, verbatim or raw).
                //
                // Skip past the leading and trailing delimiters and add the span in between.
                if (IsRawStringLiteral(literal))
                {
                    return ImmutableArray.Create(GetRawStringLiteralTextContentSpan(text, literal));
                }
                else
                {
                    var start = stringExpression.SpanStart;
                    if (start < text.Length && text[start] == '@')
                        start++;

                    if (start < text.Length && text[start] == '"')
                        start++;

                    var end = stringExpression.Span.End;
                    if (end > start && text[end - 1] == '"')
                        end--;

                    return ImmutableArray.Create(TextSpan.FromBounds(start, end));
                }
            }
            else if (stringExpression is InterpolatedStringExpressionSyntax interpolatedString)
            {
                // Interpolated string.  Normal, verbatim, or raw.
                //
                // Skip past the leading and trailing delimiters.
                var start = stringExpression.SpanStart;
                while (start < text.Length && text[start] is '@' or '$')
                    start++;

                while (start < interpolatedString.StringStartToken.Span.End && text[start] == '"')
                    start++;

                var end = stringExpression.Span.End;
                while (end > interpolatedString.StringEndToken.Span.Start && text[end - 1] == '"')
                    end--;

                // Then walk the body of the interpolated string adding (possibly empty) spans for each chunk between
                // interpolations.
                using var result = TemporaryArray<TextSpan>.Empty;

                var currentPosition = start;
                for (var i = 0; i < interpolatedString.Contents.Count; i++)
                {
                    var content = interpolatedString.Contents[i];
                    if (content is InterpolationSyntax)
                    {
                        result.Add(TextSpan.FromBounds(currentPosition, content.SpanStart));
                        currentPosition = content.Span.End;
                    }
                }

                // Then, once through the body, add a final span from the end of the last interpolation to the end delimiter.
                result.Add(TextSpan.FromBounds(currentPosition, end));
                return result.ToImmutableAndClear();
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(stringExpression);
            }
        }

        /// <summary>
        /// Returns the section of a raw string literal between the <c>"""</c> delimiters.  This also includes the
        /// leading/trailing whitespace between the delimiters for a multi-line raw string literal.
        /// </summary>
        public static TextSpan GetRawStringLiteralTextContentSpan(
            SourceText text,
            LiteralExpressionSyntax stringExpression,
            out int delimiterQuoteCount)
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

        /// <summary>
        /// Given a section of a document, finds the longest sequence of quote (<c>"</c>) characters in it.  Used to
        /// determine if a raw string literal needs to grow its delimiters to ensure that the quote sequence will no
        /// longer be a problem.
        /// </summary>
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

        public static ExpressionSyntax? FindContainingStringExpression(
            SyntaxNode root, NormalizedSnapshotSpanCollection selectionsBeforePaste)
        {
            ExpressionSyntax? expression = null;
            foreach (var snapshotSpan in selectionsBeforePaste)
            {
                var container = FindContainingStringExpression(root, snapshotSpan.Start.Position);
                if (container == null)
                    return null;

                expression ??= container;
                if (expression != container)
                    return null;
            }

            return expression;
        }

        public static ExpressionSyntax? FindContainingStringExpression(SyntaxNode root, int position)
        {
            var node = root.FindToken(position).Parent;
            for (var current = node; current != null; current = current.Parent)
            {
                if (current is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literalExpression)
                    return literalExpression;

                if (current is InterpolatedStringExpressionSyntax interpolatedString)
                    return interpolatedString;
            }

            return null;
        }

        public static string EscapeForNonRawStringLiteral(bool isVerbatim, string value)
        {
            if (isVerbatim)
                return value.Replace("\"", "\"\"");

            using var _ = PooledStringBuilder.GetInstance(out var builder);

            // taken from object-display
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Surrogate)
                {
                    var category = CharUnicodeInfo.GetUnicodeCategory(value, i);
                    if (category == UnicodeCategory.Surrogate)
                    {
                        // an unpaired surrogate
                        builder.Append("\\u" + ((int)c).ToString("x4"));
                    }
                    else if (NeedsEscaping(category))
                    {
                        // a surrogate pair that needs to be escaped
                        var unicode = char.ConvertToUtf32(value, i);
                        builder.Append("\\U" + unicode.ToString("x8"));
                        i++; // skip the already-encoded second surrogate of the pair
                    }
                    else
                    {
                        // copy a printable surrogate pair directly
                        builder.Append(c);
                        builder.Append(value[++i]);
                    }
                }
                else if (TryReplaceChar(c, out var replaceWith))
                {
                    builder.Append(replaceWith);
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();

            static bool TryReplaceChar(char c, [NotNullWhen(true)] out string? replaceWith)
            {
                replaceWith = null;
                switch (c)
                {
                    case '\\':
                        replaceWith = "\\\\";
                        break;
                    case '\0':
                        replaceWith = "\\0";
                        break;
                    case '\a':
                        replaceWith = "\\a";
                        break;
                    case '\b':
                        replaceWith = "\\b";
                        break;
                    case '\f':
                        replaceWith = "\\f";
                        break;
                    case '\n':
                        replaceWith = "\\n";
                        break;
                    case '\r':
                        replaceWith = "\\r";
                        break;
                    case '\t':
                        replaceWith = "\\t";
                        break;
                    case '\v':
                        replaceWith = "\\v";
                        break;
                    case '"':
                        replaceWith = "\\\"";
                        break;
                }

                if (replaceWith != null)
                    return true;

                if (NeedsEscaping(CharUnicodeInfo.GetUnicodeCategory(c)))
                {
                    replaceWith = "\\u" + ((int)c).ToString("x4");
                    return true;
                }

                return false;
            }

            static bool NeedsEscaping(UnicodeCategory category)
            {
                switch (category)
                {
                    case UnicodeCategory.Control:
                    case UnicodeCategory.OtherNotAssigned:
                    case UnicodeCategory.ParagraphSeparator:
                    case UnicodeCategory.LineSeparator:
                    case UnicodeCategory.Surrogate:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Given a set of source text lines, determines what common whitespace prefix each line has.  Note that this
        /// does *not* include the first line as it's super common for someone to copy a set of lines while only
        /// starting the selection at the start of the content on the first line.  This also does not include empty
        /// lines as they're also very common, but are clearly not a way of indicating indentation indent for the normal
        /// lines.
        /// </summary>
        public static string? GetCommonIndentationPrefix(SourceText text)
        {
            string? commonIndentPrefix = null;

            for (int i = 1, n = text.Lines.Count; i < n; i++)
            {
                var line = text.Lines[i];
                var nonWhitespaceIndex = GetFirstNonWhitespaceIndex(text, line);
                if (nonWhitespaceIndex >= 0)
                    commonIndentPrefix = GetCommonIndentationPrefix(commonIndentPrefix, text, TextSpan.FromBounds(line.Start, nonWhitespaceIndex));
            }

            return commonIndentPrefix;
        }

        private static string? GetCommonIndentationPrefix(string? commonIndentPrefix, SourceText text, TextSpan lineWhitespaceSpan)
        {
            // first line with indentation whitespace we're seeing.  Just keep track of that.
            if (commonIndentPrefix == null)
                return text.ToString(lineWhitespaceSpan);

            // we have indentation whitespace from a previous line.  Figure out the max commonality between it and the
            // line we're currently looking at.
            var commonPrefixLength = 0;
            for (var n = Math.Min(commonIndentPrefix.Length, lineWhitespaceSpan.Length); commonPrefixLength < n; commonPrefixLength++)
            {
                if (commonIndentPrefix[commonPrefixLength] != text[lineWhitespaceSpan.Start + commonPrefixLength])
                    break;
            }

            return commonIndentPrefix[..commonPrefixLength];
        }

        public static TextSpan MapSpan(TextSpan span, ITextSnapshot from, ITextSnapshot to)
            => from.CreateTrackingSpan(span.ToSpan(), SpanTrackingMode.EdgeInclusive).GetSpan(to).Span.ToTextSpan();
    }
}
