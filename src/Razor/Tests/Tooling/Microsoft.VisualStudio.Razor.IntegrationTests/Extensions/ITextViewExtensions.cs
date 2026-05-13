// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Razor.IntegrationTests.Extensions;

internal static class ITextViewExtensions
{
    public static SnapshotPoint? GetCaretPoint(this ITextView textView, Predicate<ITextSnapshot> match)
    {
        var caret = textView.Caret.Position;
        var span = textView.BufferGraph.MapUpOrDownToFirstMatch(new SnapshotSpan(caret.BufferPosition, 0), match);
        if (span.HasValue)
        {
            return span.Value.Start;
        }
        else
        {
            return null;
        }
    }

    public static ITextBuffer? GetBufferContainingCaret(this ITextView textView, string contentType = StandardContentTypeNames.Text)
    {
        var point = GetCaretPoint(textView, s => s.ContentType.IsOfType(contentType));
        return point.HasValue ? point.Value.Snapshot.TextBuffer : null;
    }
}
