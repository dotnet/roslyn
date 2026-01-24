// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Copilot;

internal static class CopilotUtilities
{
    /// <summary>
    /// Returns a new <see cref="SourceText"/> that represents the text after applying the specified changes to
    /// <paramref name="oldText"/>. <c>'newSpans'</c> contains the spans of the <paramref name="changes"/>
    /// mapped to the new text.  The spans are in the same order as the changes, are guaranteed to be non-overlapping.
    /// </summary>
    public static (SourceText newText, ImmutableArray<TextSpan> newSpans) GetNewTextAndChangedSpans(
        SourceText oldText, ImmutableArray<TextChange> changes)
    {
        if (changes.IsDefaultOrEmpty)
        {
            return (oldText, ImmutableArray<TextSpan>.Empty);
        }

        // Fork the starting document with the changes copilot wants to make.  Keep track of where the edited spans
        // move to in the forked doucment, as that is what we will want to analyze.
        var newText = oldText.WithChanges(changes);

        return (newText, GetTextSpansFromTextChanges(changes));
    }

    public static ImmutableArray<TextSpan> GetTextSpansFromTextChanges(IEnumerable<TextChange>? changes)
    {
        if (changes is null)
            return ImmutableArray<TextSpan>.Empty;

        var totalDelta = 0;

        var newSpans = ImmutableArray.CreateBuilder<TextSpan>();
        foreach (var change in changes)
        {
            var newTextLength = change.NewText!.Length;

            newSpans.Add(new TextSpan(change.Span.Start + totalDelta, newTextLength));
            totalDelta += newTextLength - change.Span.Length;
        }

        return newSpans.ToImmutable();
    }

    /// <summary>
    /// Returns the provided <paramref name="textChanges"/> in normalized form.  Normalized means that the
    /// changes are in order, and no change overlaps with another change.  If changes do overlap, then this
    /// returns <see langword="default"/>.  Note: abutting changes are not merged.  This allows consumers
    /// to maintain a 1:1 mapping between the changes applied to the original document and the spans in the
    /// new document.
    /// </summary>
    public static ImmutableArray<TextChange> TryNormalizeCopilotTextChanges(IEnumerable<TextChange> textChanges)
    {
        using var _ = ArrayBuilder<TextChange>.GetInstance(out var builder);
        foreach (var textChange in textChanges)
            builder.Add(textChange);

        // Ensure everything is sorted.
        builder.Sort(static (c1, c2) => c1.Span.Start - c2.Span.Start);

        // Now, go through and make sure no edit overlaps another.
        for (int i = 1, n = builder.Count; i < n; i++)
        {
            var lastEdit = builder[i - 1];
            var currentEdit = builder[i];

            if (lastEdit.Span.OverlapsWith(currentEdit.Span))
                return default;
        }

        // Things look good.  Can process these sorted edits.
        return builder.ToImmutableAndClear();
    }

    public static void ThrowIfNotNormalized(ImmutableArray<TextChange> textChanges)
    {
        Contract.ThrowIfTrue(!textChanges.IsSorted(static (c1, c2) => c1.Span.Start - c2.Span.Start), "'changes' was not sorted.");

        for (int i = 1, n = textChanges.Length; i < n; i++)
        {
            var lastEdit = textChanges[i - 1];
            var currentEdit = textChanges[i];
            Contract.ThrowIfTrue(lastEdit.Span.OverlapsWith(currentEdit.Span), "'changes' contained overlapping edits.");
        }
    }
}
