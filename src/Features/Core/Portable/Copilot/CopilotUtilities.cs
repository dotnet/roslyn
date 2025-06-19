// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Copilot;

internal static class CopilotUtilities
{
    /// <summary>
    /// Returns a new <see cref="SourceText"/> that represents the text after applying the specified changes to
    /// <paramref name="oldText"/>.  <paramref name="newSpans"/> contains the spans of the <paramref name="changes"/>
    /// mapped to the new text.  The spans are in the same order as the changes, are guaranteed to be non-overlapping.
    /// </summary>
    public static SourceText GetNewText(
        SourceText oldText, ImmutableArray<TextChange> changes, ArrayBuilder<TextSpan> newSpans)
    {
        // Fork the starting document with the changes copilot wants to make.  Keep track of where the edited spans
        // move to in the forked doucment, as that is what we will want to analyze.
        var newText = oldText.WithChanges(changes);

        var totalDelta = 0;

        foreach (var change in changes)
        {
            var newTextLength = change.NewText!.Length;

            newSpans.Add(new TextSpan(change.Span.Start + totalDelta, newTextLength));
            totalDelta += newTextLength - change.Span.Length;
        }

        return newText;
    }

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
        Contract.ThrowIfTrue(new NormalizedTextSpanCollection(textChanges.Select(c => c.Span)).Count != textChanges.Length, "'changes' was not normalized.");
    }
}
