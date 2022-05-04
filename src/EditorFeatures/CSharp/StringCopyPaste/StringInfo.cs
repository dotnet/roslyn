// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste;

using static StringCopyPasteHelpers;

internal readonly struct StringInfo
{
    /// <summary>
    /// Number of quotes in the delimiter of the string being pasted into.  Given that the string should have no errors
    /// in it, this quote count should be the same for the start and end delimiter.This will be <c>1</c> for
    /// non-raw-strings, and will be 3-or-more for raw-strings
    /// </summary>
    public readonly int DelimiterQuoteCount;

    /// <summary>
    /// Number of dollar signs (<c>$</c>) in the starting delimiter of the string being pasted into. This will be
    /// <c>1</c> for non-raw-strings, and will be 1-or-more for raw-interpolated-strings.
    /// </summary>
    public readonly int DelimiterDollarCount;

    /// <summary>
    /// The span of the starting delimiter quotes (including characters like <c>$</c> or <c>@</c>)
    /// </summary>
    public readonly TextSpan StartDelimiterSpan;

    /// <summary>
    /// The span of the ending delimiter quotes (including a suffix like <c>u8</c>)
    /// </summary>
    public readonly TextSpan EndDelimiterSpan;

    /// <summary>
    /// Spans of text-content within the string.  These represent the spans where text can go within a string
    /// literal/interpolation.  Note that these spans may be empty.  For example, this happens for cases like the empty
    /// string <c>""</c>, or between interpolation holes like <c>$"x{a}{b}y"</c>. These spans can be examined to
    /// determine if pasted content is only impacting the content portion of a string, and not the delimiters or
    /// interpolation-holes. For raw strings, this will include the whitespace and newlines after the starting quotes
    /// and before the ending quotes.
    /// </summary>
    public readonly ImmutableArray<TextSpan> ContentSpans;

    /// <summary>
    /// Similar to <see cref="ContentSpans"/> except that whitespace in a raw-string-literal that is not considered part
    /// of the final content will not be included.
    /// </summary>
    public readonly ImmutableArray<TextSpan> WithoutIndentationContentSpans;

    public StringInfo(
        int delimiterQuoteCount,
        int delimiterDollarCount,
        TextSpan startDelimiterSpan,
        TextSpan endDelimiterSpan,
        ImmutableArray<TextSpan> contentSpans,
        ImmutableArray<TextSpan> withoutIndentationContentSpans)
    {
        DelimiterQuoteCount = delimiterQuoteCount;
        DelimiterDollarCount = delimiterDollarCount;
        StartDelimiterSpan = startDelimiterSpan;
        EndDelimiterSpan = endDelimiterSpan;
        ContentSpans = contentSpans;
        WithoutIndentationContentSpans = withoutIndentationContentSpans;
    }

    public static StringInfo GetStringInfo(SourceText text, ExpressionSyntax stringExpression)
        => stringExpression switch
        {
            LiteralExpressionSyntax literal => GetStringLiteralInfo(text, literal),
            InterpolatedStringExpressionSyntax interpolatedString => GetInterpolatedStringInfo(text, interpolatedString),
            _ => throw ExceptionUtilities.UnexpectedValue(stringExpression)
        };

    private static StringInfo GetStringLiteralInfo(SourceText text, LiteralExpressionSyntax literal)
    {
        // simple string literal (normal, verbatim or raw).
        //
        // Skip past the leading and trailing delimiters and add the span in between.
        //
        // The two cases look similar but are subtly different.  Ignoring the fact that raw strings don't start with
        // '@', there's also the issue that normal strings just have a single starting/ending quote, where as
        // raw-strings can have an unbounded number of them.
        return IsRawStringLiteral(literal)
            ? GetRawStringLiteralInfo(text, literal)
            : GetNormalStringLiteralStringInfo(text, literal);
    }

    private static StringInfo GetRawStringLiteralInfo(SourceText text, LiteralExpressionSyntax literal)
    {
        var start = literal.SpanStart;
        while (SafeCharAt(text, start) == '"')
            start++;
        var delimiterQuoteCount = start - literal.SpanStart;

        var end = literal.Span.End;

        end = SkipU8Suffix(text, end);
        while (end > start && text[end - 1] == '"')
            end--;

        if (literal.Token.Kind() is SyntaxKind.SingleLineRawStringLiteralToken)
        {
            var contentSpans = ImmutableArray.Create(TextSpan.FromBounds(start, end));

            // A single line raw literal doesn't have any indentation processing.  So we use the same spans for both
            // sets of content.
            return new StringInfo(
                delimiterQuoteCount, delimiterDollarCount: 0,
                startDelimiterSpan: TextSpan.FromBounds(literal.SpanStart, start),
                endDelimiterSpan: TextSpan.FromBounds(end, literal.Span.End),
                contentSpans,
                withoutIndentationContentSpans: contentSpans);
        }
        else if (literal.Token.Kind() is SyntaxKind.MultiLineRawStringLiteralToken)
        {
            // Consume the whitespace and newline after the initial quotes.
            var rawStart = start;
            while (SyntaxFacts.IsWhitespace(SafeCharAt(text, rawStart)))
                rawStart++;

            if (SafeCharAt(text, rawStart) == '\r' && SafeCharAt(text, rawStart + 1) == '\n')
            {
                rawStart += 2;
            }
            else
            {
                // Guaranteed by us not having any syntax errors on the node.
                Contract.ThrowIfFalse(SyntaxFacts.IsNewLine(text[rawStart]));
                rawStart++;
            }

            // Consume the whitespace and newline preceding the end quote.
            var rawEnd = end;
            while (SyntaxFacts.IsWhitespace(SafeCharAt(text, rawEnd - 1)))
                rawEnd--;

            var indentationLength = end - rawEnd;

            if (SafeCharAt(text, rawEnd - 2) == '\r' && SafeCharAt(text, rawEnd - 1) == '\n')
            {
                rawEnd -= 2;
            }
            else
            {
                Contract.ThrowIfFalse(SyntaxFacts.IsNewLine(text[rawEnd - 1]));
                rawEnd--;
            }

            using var _ = ArrayBuilder<TextSpan>.GetInstance(out var withoutIndentationBuilder);

            var contentLineStart = text.Lines.GetLineFromPosition(rawStart).LineNumber;
            var contentLineEnd = text.Lines.GetLineFromPosition(rawEnd).LineNumber;

            for (var currentLineNumber = contentLineStart; currentLineNumber <= contentLineEnd; currentLineNumber++)
            {
                var currentLine = text.Lines[currentLineNumber];
                if (currentLine.Span.Length < indentationLength)
                {
                    // this is a blank line.  Don't include any of it in the content spans except its newline.
                    withoutIndentationBuilder.Add(TextSpan.FromBounds(currentLine.Span.End, currentLine.SpanIncludingLineBreak.End));
                }
                else
                {
                    // Normal line, include the contents after the indentation whitespace.  If this is not the last
                    // line, include the linebreak as well, otherwise ignore it (it's not part of a raw string's
                    // content).
                    withoutIndentationBuilder.Add(TextSpan.FromBounds(
                        start: currentLine.Start + indentationLength,
                        end: currentLineNumber == contentLineEnd ? currentLine.Span.End : currentLine.SpanIncludingLineBreak.End));
                }
            }

            return new StringInfo(
                delimiterQuoteCount,
                delimiterDollarCount: 0,
                TextSpan.FromBounds(literal.SpanStart, rawStart),
                TextSpan.FromBounds(rawEnd, literal.Span.End),
                contentSpans: ImmutableArray.Create(TextSpan.FromBounds(start, end)),
                withoutIndentationContentSpans: withoutIndentationBuilder.ToImmutable());
        }
        else
        {
            throw ExceptionUtilities.UnexpectedValue(literal.Kind());
        }
    }

    private static StringInfo GetNormalStringLiteralStringInfo(SourceText text, LiteralExpressionSyntax literal)
    {
        var start = literal.SpanStart;
        if (SafeCharAt(text, start) == '@')
            start++;

        var position = start;
        if (SafeCharAt(text, start) == '"')
            start++;
        var delimiterQuoteCount = start - position;

        var end = literal.Span.End;

        end = SkipU8Suffix(text, end);
        if (end > start && text[end - 1] == '"')
            end--;

        var contentSpans = ImmutableArray.Create(TextSpan.FromBounds(start, end));

        return new StringInfo(
            delimiterQuoteCount,
            delimiterDollarCount: 0,
            startDelimiterSpan: TextSpan.FromBounds(literal.SpanStart, start),
            endDelimiterSpan: TextSpan.FromBounds(end, literal.Span.End),
            contentSpans,
            contentSpans);
    }

    private static StringInfo GetInterpolatedStringInfo(
        SourceText text, InterpolatedStringExpressionSyntax interpolatedString)
    {
        // Interpolated string.  Normal, verbatim, or raw.
        //
        // Skip past the leading and trailing delimiters.
        var start = interpolatedString.SpanStart;
        while (SafeCharAt(text, start) is '@' or '$')
            start++;
        var delimiterDollarCount = start - interpolatedString.SpanStart;

        var position = start;
        while (start < interpolatedString.StringStartToken.Span.End && text[start] == '"')
            start++;
        var delimiterQuoteCount = start - position;

        var end = interpolatedString.Span.End;

        end = SkipU8Suffix(text, end);
        while (end > interpolatedString.StringEndToken.Span.Start && text[end - 1] == '"')
            end--;

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

        var startDelimiterSpan = TextSpan.FromBounds(interpolatedString.SpanStart, interpolatedString.StringStartToken.Span.End);
        var endDelimiterSpan = TextSpan.FromBounds(interpolatedString.StringEndToken.SpanStart, interpolatedString.Span.End);

        // Then, once through the body, add a final span from the end of the last interpolation to the end delimiter.
        result.Add(TextSpan.FromBounds(currentPosition, end));
        var contentSpans = result.ToImmutableAndClear();

        // todo: implement withoutIndentationSpans properly
        var withoutIndentationSpans = contentSpans;

        return new StringInfo(delimiterQuoteCount, delimiterDollarCount, startDelimiterSpan, endDelimiterSpan, contentSpans, withoutIndentationSpans);
    }
}
