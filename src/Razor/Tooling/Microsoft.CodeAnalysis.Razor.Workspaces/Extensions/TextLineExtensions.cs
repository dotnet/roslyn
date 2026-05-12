// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Razor;

namespace Microsoft.CodeAnalysis.Text;

internal static class TextLineExtensions
{
    public static char CharAt(this TextLine line, int offset)
    {
        ArgHelper.ThrowIfNegative(offset);
        ArgHelper.ThrowIfGreaterThanOrEqual(offset, line.SpanIncludingLineBreak.Length);

        var text = line.Text.AssumeNotNull();
        var index = line.Start + offset;
        Debug.Assert(index < text.Length, "This should be impossible as we validated offset against the line length above.");

        return text[index];
    }

    public static string GetLeadingWhitespace(this TextLine line)
    {
        return line.GetFirstNonWhitespaceOffset() is int offset
            ? line.Text.AssumeNotNull().ToString(TextSpan.FromBounds(line.Start, line.Start + offset))
            : string.Empty;
    }

    public static int GetIndentationSize(this TextLine line, long tabSize)
    {
        var text = line.Text.AssumeNotNull();

        var indentation = 0;
        for (var i = line.Start; i < line.End; i++)
        {
            var c = text[i];
            if (c == '\t')
            {
                indentation += (int)tabSize;
            }
            else if (char.IsWhiteSpace(c))
            {
                indentation++;
            }
            else
            {
                break;
            }
        }

        return indentation;
    }

    public static int? GetFirstNonWhitespacePosition(this TextLine line)
    {
        var firstNonWhitespaceOffset = line.GetFirstNonWhitespaceOffset();

        return firstNonWhitespaceOffset.HasValue
            ? firstNonWhitespaceOffset + line.Start
            : null;
    }

    public static int? GetFirstNonWhitespaceOffset(this TextLine line, int startOffset = 0)
    {
        if (startOffset > line.SpanIncludingLineBreak.Length)
        {
            return ThrowHelper.ThrowArgumentOutOfRangeException<int?>(nameof(startOffset), SR.Invalid_Offset);
        }

        return line.Text!.TryGetFirstNonWhitespaceOffset(TextSpan.FromBounds(line.Start + startOffset, line.EndIncludingLineBreak), out var offset)
            ? offset
            : null;
    }
}
