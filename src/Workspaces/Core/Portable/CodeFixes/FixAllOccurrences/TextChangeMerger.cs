// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes;

/// <summary>
/// Helper to merge many disparate text changes to a single document together into a total set of changes.
/// </summary>
internal class TextChangeMerger
{
    private readonly struct IntervalIntrospector : IIntervalIntrospector<TextChange>
    {
        public TextSpan GetSpan(TextChange value)
            => value.Span;
    }

    private readonly Document _oldDocument;
    private readonly IDocumentTextDifferencingService _differenceService;

    private readonly SimpleMutableIntervalTree<TextChange, IntervalIntrospector> _totalChangesIntervalTree =
        BinaryIntervalTree.Create(new IntervalIntrospector(), Array.Empty<TextChange>());

    public TextChangeMerger(Document document)
    {
        _oldDocument = document;
        _differenceService = document.Project.Solution.Services.GetRequiredService<IDocumentTextDifferencingService>();
    }

    /// <summary>
    /// Try to merge the changes made to <paramref name="newDocument"/> into the tracked changes. If there is any
    /// conflicting change in <paramref name="newDocument"/> with existing changes, then no changes are added.
    /// </summary>
    public async Task TryMergeChangesAsync(Document newDocument, CancellationToken cancellationToken)
    {
        Debug.Assert(newDocument.Id == _oldDocument.Id);

        cancellationToken.ThrowIfCancellationRequested();
        var currentChanges = await _differenceService.GetTextChangesAsync(
            _oldDocument, newDocument, cancellationToken).ConfigureAwait(false);

        if (AllChangesCanBeApplied(_totalChangesIntervalTree, currentChanges))
        {
            foreach (var change in currentChanges)
                _totalChangesIntervalTree.AddIntervalInPlace(change);
        }
    }

    /// <summary>
    /// Try to merge the changes made to all the documents in <paramref name="newDocuments"/> in order into the
    /// tracked changes. If there is any conflicting changes with existing changes for a particular document, then
    /// no changes will be added for it.
    /// </summary>
    public async Task TryMergeChangesAsync(ImmutableArray<Document> newDocuments, CancellationToken cancellationToken)
    {
        foreach (var newDocument in newDocuments)
            await TryMergeChangesAsync(newDocument, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SourceText> GetFinalMergedTextAsync(CancellationToken cancellationToken)
    {
        // WithChanges requires a ordered list of TextChanges without any overlap.
        var changesToApply = _totalChangesIntervalTree.Distinct().OrderBy(tc => tc.Span.Start);

        var oldText = await _oldDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var newText = oldText.WithChanges(changesToApply);

        return newText;
    }

    private static bool AllChangesCanBeApplied(
        SimpleMutableIntervalTree<TextChange, IntervalIntrospector> cumulativeChanges,
        ImmutableArray<TextChange> currentChanges)
    {
        using var overlappingSpans = TemporaryArray<TextChange>.Empty;
        using var intersectingSpans = TemporaryArray<TextChange>.Empty;

        return AllChangesCanBeApplied(
            cumulativeChanges, currentChanges,
            overlappingSpans: ref overlappingSpans.AsRef(),
            intersectingSpans: ref intersectingSpans.AsRef());
    }

    private static bool AllChangesCanBeApplied(
        SimpleMutableIntervalTree<TextChange, IntervalIntrospector> cumulativeChanges,
        ImmutableArray<TextChange> currentChanges,
        ref TemporaryArray<TextChange> overlappingSpans,
        ref TemporaryArray<TextChange> intersectingSpans)
    {
        foreach (var change in currentChanges)
        {
            overlappingSpans.Clear();
            intersectingSpans.Clear();

            cumulativeChanges.FillWithIntervalsThatOverlapWith(
                change.Span.Start, change.Span.Length, ref overlappingSpans);

            cumulativeChanges.FillWithIntervalsThatIntersectWith(
               change.Span.Start, change.Span.Length, ref intersectingSpans);

            var value = ChangeCanBeApplied(change,
                overlappingSpans: in overlappingSpans,
                intersectingSpans: in intersectingSpans);
            if (!value)
            {
                return false;
            }
        }

        // All the changes would merge in fine.  We can absorb this.
        return true;
    }

    private static bool ChangeCanBeApplied(
        TextChange change,
        in TemporaryArray<TextChange> overlappingSpans,
        in TemporaryArray<TextChange> intersectingSpans)
    {
        // We distinguish two types of changes that can happen.  'Pure Insertions' 
        // and 'Overwrites'.  Pure-Insertions are those that are just inserting 
        // text into a specific *position*.  They do not replace any existing text.
        // 'Overwrites' end up replacing existing text with some other piece of 
        // (possibly-empty) text.
        //
        // Overwrites of text tend to be easy to understand and merge.  It is very
        // clear what code is being overwritten and how it should interact with
        // other changes.  Pure-insertions are more ambiguous to deal with.  For
        // example, say there are two pure-insertions at some position.  There is
        // no way for us to know what to do with this.  For example, we could take
        // one insertion then the other, or vice versa.  Because of this ambiguity
        // we conservatively disallow cases like this.

        return IsPureInsertion(change)
            ? PureInsertionChangeCanBeApplied(change, in overlappingSpans, in intersectingSpans)
            : OverwriteChangeCanBeApplied(change, in overlappingSpans, in intersectingSpans);
    }

    private static bool IsPureInsertion(TextChange change)
        => change.Span.IsEmpty;

    private static bool PureInsertionChangeCanBeApplied(
        TextChange change,
        in TemporaryArray<TextChange> overlappingSpans,
        in TemporaryArray<TextChange> intersectingSpans)
    {
        // Pure insertions can't ever overlap anything.  (They're just an insertion at a 
        // single position, and overlaps can't occur with single-positions).
        Debug.Assert(IsPureInsertion(change));
        Debug.Assert(overlappingSpans.Count == 0);
        if (intersectingSpans.Count == 0)
        {
            // Our pure-insertion didn't hit any other changes.  This is safe to apply.
            return true;
        }

        if (intersectingSpans.Count == 1)
        {
            // Our pure-insertion hit another change.  Thats safe when:
            //  1) if both changes are the same.
            //  2) the change we're hitting is an overwrite-change and we're at the end of it.

            // Specifically, it is not safe for us to insert somewhere in start-to-middle of an 
            // existing overwrite-change.  And if we have another pure-insertion change, then it's 
            // not safe for both of us to be inserting at the same point (except when the 
            // change is identical).

            // Note: you may wonder why we don't support hitting an overwriting change at the
            // start of the overwrite.  This is because it's now ambiguous as to which of these
            // changes should be applied first.

            var otherChange = intersectingSpans[0];
            if (otherChange == change)
            {
                // We're both pure-inserting the same text at the same position.  
                // We assume this is a case of some provider making the same changes and
                // we allow this.
                return true;
            }

            return !IsPureInsertion(otherChange) &&
                   otherChange.Span.End == change.Span.Start;
        }

        // We're intersecting multiple changes.  That's never OK.
        return false;
    }

    private static bool OverwriteChangeCanBeApplied(
        TextChange change,
        in TemporaryArray<TextChange> overlappingSpans,
        in TemporaryArray<TextChange> intersectingSpans)
    {
        Debug.Assert(!IsPureInsertion(change));

        return !OverwriteChangeConflictsWithOverlappingSpans(change, in overlappingSpans) &&
               !OverwriteChangeConflictsWithIntersectingSpans(change, in intersectingSpans);
    }

    private static bool OverwriteChangeConflictsWithOverlappingSpans(
        TextChange change,
        in TemporaryArray<TextChange> overlappingSpans)
    {
        Debug.Assert(!IsPureInsertion(change));

        if (overlappingSpans.Count == 0)
        {
            // This overwrite didn't overlap with any other changes.  This change is safe to make.
            return false;
        }

        // The change we want to make overlapped an existing change we're making.  Only allow
        // this if there was a single overlap and we are exactly the same change as it.
        // Otherwise, this is a conflict.
        var isSafe = overlappingSpans.Count == 1 && overlappingSpans[0] == change;

        return !isSafe;
    }

    private static bool OverwriteChangeConflictsWithIntersectingSpans(
        TextChange change,
        in TemporaryArray<TextChange> intersectingSpans)
    {
        Debug.Assert(!IsPureInsertion(change));

        // We care about our intersections with pure-insertion changes.  Overwrite-changes that
        // we overlap are already handled in OverwriteChangeConflictsWithOverlappingSpans.
        // And overwrite spans that we abut (i.e. which we're adjacent to) are totally safe 
        // for both to be applied.
        //
        // However, pure-insertion changes are extremely ambiguous. It is not possible to tell which
        // change should be applied first.  So if we get any pure-insertions we have to bail
        // on applying this span.
        return intersectingSpans.Any(static otherSpan => IsPureInsertion(otherSpan));
    }
}
