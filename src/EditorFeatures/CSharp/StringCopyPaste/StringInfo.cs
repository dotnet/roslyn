// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste;

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
}
