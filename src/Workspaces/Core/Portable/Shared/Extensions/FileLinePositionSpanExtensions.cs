// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class FileLinePositionSpanExtensions
{
    /// <inheritdoc cref="LinePositionSpanExtensions.GetClampedTextSpan"/>
    public static TextSpan GetClampedTextSpan(this FileLinePositionSpan span, SourceText text)
        => span.Span.GetClampedTextSpan(text);

    /// <inheritdoc cref="LinePositionSpanExtensions.GetClampedSpan"/>
    public static LinePositionSpan GetClampedSpan(this FileLinePositionSpan span, SourceText text)
        => span.Span.GetClampedSpan(text);
}

internal static class LinePositionSpanExtensions
{
    /// <summary>
    /// Returns a new <see cref="TextSpan"/> based off of the positions in <paramref name="span"/>, but
    /// which is guaranteed to fall entirely within the span of <paramref name="text"/>.
    /// </summary>
    public static TextSpan GetClampedTextSpan(this LinePositionSpan span, SourceText text)
    {
        var clampedSpan = span.GetClampedSpan(text);
        return text.Lines.GetTextSpan(clampedSpan);
    }

    /// <summary>
    /// Returns a new <see cref="LinePositionSpan"/> based off of the positions in <paramref name="span"/>, but
    /// which is guaranteed to fall entirely within the span of <paramref name="text"/>.
    /// </summary>
    public static LinePositionSpan GetClampedSpan(this LinePositionSpan span, SourceText text)
    {
        var lines = text.Lines;
        if (lines.Count == 0)
            return default;

        var startLine = span.Start.Line;
        var endLine = span.End.Line;

        // Make sure the starting columns are never negative.
        var startColumn = Math.Max(span.Start.Character, 0);
        var endColumn = Math.Max(span.End.Character, 0);

        if (startLine < 0)
        {
            // If the start line is negative (e.g. before the start of the actual document) then move the start to the 0,0 position.
            startLine = 0;
            startColumn = 0;
        }
        else if (startLine >= lines.Count)
        {
            // if the start line is after the end of the document, move the start to the last location in the document.
            startLine = lines.Count - 1;
            startColumn = lines[startLine].SpanIncludingLineBreak.Length;
        }

        if (endLine < 0)
        {
            // if the end is before the start of the document, then move the end to wherever the start position was determined to be.
            endLine = startLine;
            endColumn = startColumn;
        }
        else if (endLine >= lines.Count)
        {
            // if the end line is after the end of the document, move the end to the last location in the document.
            endLine = lines.Count - 1;
            endColumn = lines[endLine].SpanIncludingLineBreak.Length;
        }

        // now, ensure that the column of the start/end positions is within the length of its line.
        startColumn = Math.Min(startColumn, lines[startLine].SpanIncludingLineBreak.Length);
        endColumn = Math.Min(endColumn, lines[endLine].SpanIncludingLineBreak.Length);

        var start = new LinePosition(startLine, startColumn);
        var end = new LinePosition(endLine, endColumn);

        // swap if necessary
        if (end < start)
            (start, end) = (end, start);

        // Validate that the points are actually within the span of the text.
        Contract.ThrowIfTrue(start < text.Lines.GetLinePosition(0));
        Contract.ThrowIfTrue(end > text.Lines.GetLinePosition(text.Length));
        return new LinePositionSpan(start, end);
    }
}
