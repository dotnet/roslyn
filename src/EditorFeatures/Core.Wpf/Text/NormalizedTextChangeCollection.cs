using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Utilities;

#nullable disable

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal partial class NormalizedTextChangeCollection : INormalizedTextChangeCollection
    {
        public static readonly NormalizedTextChangeCollection Empty = new(Array.Empty<ITextChange>());
        private static readonly TimeSpan s_minimalEditDiffTimeoutThreshold = TimeSpan.FromSeconds(2);
        private readonly IReadOnlyList<ITextChange> _changes;

        public static INormalizedTextChangeCollection Create(IReadOnlyList<ITextChange> changes)
        {
            INormalizedTextChangeCollection result = GetTrivialCollection(changes);
            return result != null ? result : new NormalizedTextChangeCollection(changes);
        }

        public static INormalizedTextChangeCollection Create(IReadOnlyList<ITextChange> changes, StringDifferenceOptions? differenceOptions, ITextDifferencingService textDifferencingService,
                                                             ITextSnapshot before = null, ITextSnapshot after = null)
        {
            INormalizedTextChangeCollection result = GetTrivialCollection(changes);
            return result != null ? result : new NormalizedTextChangeCollection(changes, differenceOptions, textDifferencingService, before, after);
        }

        private static INormalizedTextChangeCollection GetTrivialCollection(IReadOnlyList<ITextChange> changes)
        {
            if (changes == null)
            {
                throw new ArgumentNullException(nameof(changes));
            }

            if (changes.Count == 0)
            {
                return Empty;
            }
            else if (changes.Count == 1)
            {
                ITextChange tc = changes[0];
                if (tc.OldLength + tc.NewLength == 1 && tc.LineBreakBoundaryConditions == LineBreakBoundaryConditions.None &&
                    tc.LineCountDelta == 0)
                {
                    bool isInsertion = tc.NewLength == 1;
                    char data = isInsertion ? tc.NewText[0] : tc.OldText[0];
                    return new TrivialNormalizedTextChangeCollection(data, isInsertion, tc.OldPosition);
                }
            }

            return null;
        }

        /// <summary>
        /// Construct a normalized version of the given ITextChange collection,
        /// but don't compute minimal edits.
        /// </summary>
        /// <param name="changes">List of changes to normalize</param>
        private NormalizedTextChangeCollection(IReadOnlyList<ITextChange> changes)
        {
            _changes = Normalize(changes, null, null, null, null);
        }

        /// <summary>
        /// Construct a normalized version of the given ITextChange collection.
        /// </summary>
        /// <param name="changes">List of changes to normalize</param>
        /// <param name="differenceOptions">The difference options to use for minimal differencing, if any.</param>
        /// <param name="textDifferencingService">The difference service to use, if differenceOptions were supplied.</param>
        /// <param name="before">Text snapshot before the change (can be null).</param>
        /// <param name="after">Text snapshot after the change (can be null).</param>
        private NormalizedTextChangeCollection(IReadOnlyList<ITextChange> changes, StringDifferenceOptions? differenceOptions, ITextDifferencingService textDifferencingService,
                                               ITextSnapshot before, ITextSnapshot after)
        {
            _changes = Normalize(changes, differenceOptions, textDifferencingService, before, after);
        }

        public bool IncludesLineChanges
        {
            get
            {
                for (var i = 0; i < _changes.Count; i++)
                {
                    if (_changes[i].LineCountDelta != 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Normalize a sequence of changes that were all applied consecutively to the same version of a buffer. Positions of the
        /// normalized changes are adjusted to account for other changes that occur at lower indexes in the
        /// buffer, and changes are sorted and merged if possible.
        /// </summary>
        /// <param name="changes">The changes to normalize.</param>
        /// <param name="differenceOptions">The options to use for minimal differencing, if any.</param>
        /// <param name="textDifferencingService"></param>
        /// <param name="before">Text snapshot before the change (can be null).</param>
        /// <param name="after">Text snapshot after the change (can be null).</param>
        /// <returns>A (possibly empty) list of changes, sorted by Position, with adjacent and overlapping changes combined
        /// where possible.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="changes"/> is null.</exception>
        private static IReadOnlyList<ITextChange> Normalize(IReadOnlyList<ITextChange> changes, StringDifferenceOptions? differenceOptions, ITextDifferencingService textDifferencingService,
                                                            ITextSnapshot before, ITextSnapshot after)

        {
            // Diffs of long, highly random lines, can be very computationally expensive
            // and don't scale consistently with change length or line count, so they are
            // hard to predict. To combat this, enforce a timeout when applying min edits.
            if (differenceOptions.HasValue)
            {
                using var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(s_minimalEditDiffTimeoutThreshold);

                return NormalizeInternal(changes, differenceOptions, textDifferencingService, before, after, cancellationTokenSource.Token);
            }
            else
            {
                return NormalizeInternal(changes, differenceOptions, textDifferencingService, before, after, CancellationToken.None);
            }
        }

        private static IReadOnlyList<ITextChange> NormalizeInternal(
            IReadOnlyList<ITextChange> changes,
            StringDifferenceOptions? differenceOptions,
            ITextDifferencingService textDifferencingService,
            ITextSnapshot before,
            ITextSnapshot after,
            CancellationToken cancellationToken)
        {
            if (changes.Count == 1 && differenceOptions == null)
            {
                // By far the most common path
                // If we are computing minimal changes, we need to go through the
                // algorithm anyway, since this change may be split up into many
                // smaller changes
                return new ITextChange[] { changes[0] };
            }
            else if (changes.Count == 0)
            {
                return Array.Empty<ITextChange>();
            }

            ITextChange[] work = TextUtilities.StableSort(changes, ITextChange.Compare);

            // work is now sorted by increasing Position

            int accumulatedDelta = 0;
            int a = 0;
            int b = 1;
            while (b < work.Length)
            {
                // examine a pair of changes and attempt to combine them
                ITextChange aChange = work[a];
                ITextChange bChange = work[b];
                int gap = bChange.OldPosition - aChange.OldEnd;

                if (gap > 0)
                {
                    // independent changes
                    aChange.NewPosition = aChange.OldPosition + accumulatedDelta;
                    accumulatedDelta += aChange.Delta;
                    a = b;
                    b = a + 1;
                }
                else
                {
                    // dependent changes. merge all adjacent dependent changes into a single change in one pass,
                    // to avoid expensive pairwise concatenations.
                    //
                    // Use StringRebuilders (which allow strings to be concatenated without creating copies of the strings) to assemble the
                    // pieces.
                    StringRebuilder newRebuilder = aChange._newText;
                    StringRebuilder oldRebuilder = aChange._oldText;

                    int aChangeIncrementalDeletions = 0;
                    do
                    {
                        newRebuilder = newRebuilder.Append(bChange._newText);

                        if (gap == 0)
                        {
                            // abutting deletions
                            oldRebuilder = oldRebuilder.Append(bChange._oldText);
                            aChangeIncrementalDeletions += bChange.OldLength;
                            aChange.LineBreakBoundaryConditions =
                                (aChange.LineBreakBoundaryConditions & LineBreakBoundaryConditions.PrecedingReturn) |
                                (bChange.LineBreakBoundaryConditions & LineBreakBoundaryConditions.SucceedingNewline);
                        }
                        else
                        {
                            // overlapping deletions
                            if (aChange.OldEnd + aChangeIncrementalDeletions < bChange.OldEnd)
                            {
                                int overlap = aChange.OldEnd + aChangeIncrementalDeletions - bChange.OldPosition;
                                oldRebuilder = oldRebuilder.Append(bChange._oldText.GetSubText(Span.FromBounds(overlap, bChange._oldText.Length)));
                                aChangeIncrementalDeletions += (bChange.OldLength - overlap);
                                aChange.LineBreakBoundaryConditions =
                                    (aChange.LineBreakBoundaryConditions & LineBreakBoundaryConditions.PrecedingReturn) |
                                    (bChange.LineBreakBoundaryConditions & LineBreakBoundaryConditions.SucceedingNewline);
                            }
                            // else bChange deletion subsumed by aChange deletion
                        }

                        work[b] = null;
                        b++;
                        if (b == work.Length)
                        {
                            break;
                        }
                        bChange = work[b];
                        gap = bChange.OldPosition - aChange.OldEnd - aChangeIncrementalDeletions;
                    } while (gap <= 0);

                    work[a]._oldText = oldRebuilder;
                    work[a]._newText = newRebuilder;

                    if (b < work.Length)
                    {
                        aChange.NewPosition = aChange.OldPosition + accumulatedDelta;
                        accumulatedDelta += aChange.Delta;
                        a = b;
                        b = a + 1;
                    }
                }
            }
            // a points to the last surviving change
            work[a].NewPosition = work[a].OldPosition + accumulatedDelta;

            List<ITextChange> result = new List<ITextChange>();

            if (differenceOptions.HasValue)
            {
                _ = Requires.NotNull(textDifferencingService, nameof(textDifferencingService));
                foreach (ITextChange change in work)
                {
                    if (change == null)
                    {
                        continue;
                    }

                    // Make sure this is a replacement
                    if (change.OldLength == 0 || change.NewLength == 0)
                    {
                        result.Add(change);
                        continue;
                    }

                    if (change.OldLength >= TextModelOptions.DiffSizeThreshold ||
                        change.NewLength >= TextModelOptions.DiffSizeThreshold ||
                        cancellationToken.IsCancellationRequested)
                    {
                        change.IsOpaque = true;
                        result.Add(change);
                        continue;
                        // too big to even attempt a diff. This is aimed at the reload-a-giant-file scenario
                        // where OOM during diff is a distinct possibility.
                    }

                    // Make sure to turn off IgnoreTrimWhiteSpace, since that doesn't make sense in
                    // the context of a minimal edit
                    StringDifferenceOptions options = new StringDifferenceOptions(differenceOptions.Value);
                    options.IgnoreTrimWhiteSpace = false;
                    IHierarchicalDifferenceCollection diffs;

                    options.ContinueProcessingPredicate = (int leftIndex, IList<string> leftSequence, int longestMatchSoFar) =>
                    {
                        return differenceOptions.Value.ContinueProcessingPredicate != null ?
                            differenceOptions.Value.ContinueProcessingPredicate(leftIndex, leftSequence, longestMatchSoFar) :
                            !cancellationToken.IsCancellationRequested;
                    };

                    if (before != null && after != null)
                    {
                        // Don't materialize the strings when we know the before and after snapshots. They might be really huge and cause OOM.
                        // We will take this path in the file reload case.
                        diffs = textDifferencingService.DiffSnapshotSpans(new SnapshotSpan(before, change.OldSpan),
                                                                            new SnapshotSpan(after, change.NewSpan), options);
                        Debug.Assert(diffs is IHierarchicalDifferenceCollectionInternal, "Expected new interface IHierarchicalDifferenceCollectionInternal");
                        if (diffs is IHierarchicalDifferenceCollectionInternal diffsInternal)
                        {
                            // The diff calculator wasn't able to compute the first layer of differences because it was too expensive.
                            // So don't bother trying to minimize the diff further, as doing so will likely be unsuccessful as well and just
                            // waste more time (processing large files is slow!).
                            if (diffsInternal.QuitEarly)
                            {
                                result.Add(change);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        // We need to evaluate the old and new text for the differencing service
                        string oldText = change.OldText;
                        string newText = change.NewText;

                        if (string.Equals(oldText, newText, StringComparison.Ordinal))
                        {
                            // This change simply evaporates. This case occurs frequently in Venus and it is much
                            // better to short circuit it here than to fire up the differencing engine.
                            continue;
                        }
                        diffs = textDifferencingService.DiffStrings(oldText, newText, options);
                    }

                    // Keep track of deltas for the "new" position, for sanity check
                    int delta = 0;

                    // Add all the changes from the difference collection
                    var minimalChanges = GetChangesFromDifferenceCollection(ref delta, change, change._oldText, change._newText, diffs);

                    // When we are computing minimal differences (differenceOptions != null), we can have a problem when the change has almost no effect.
                    // For example, supposes we are replacing "the quick brown fox jumps over the lazy dog" with "the quick red fox jumps over the lazy dog".
                    //
                    // In that case, change._newText is "the quick red fox jumps over the lazy dog" but the actual change is replacing "brown" with "red".
                    //
                    // If we do not do anything, c._newText will actually reference a span within "the quick red fox jumps over the lazy dog" (it is, effectively,
                    // just "red" but it pins all of "the quick red fox jumps over the lazy dog" in memory). That can be bad, especially in cases like file
                    // reload where change._newText is effectively the entire contents of the reloaded version of the file.
                    //
                    // So look at add the individual changes and if the sum of their lengths (plus a fudge factor) is less than the original pinned string,
                    // it makes sense to make make copies of all the new text that does not pin the original string. This is an all or nothing thing, however:
                    // there is no point in replacing _newText of one change and letting another change continue to pin change._newText.
                    //
                    // Note there is no point in trying to make copies of change._oldText: those StringRebuilders can pin large strings but it is very likely
                    // those strings are already pinned in the by the after edit snapshot. In the case above, if the original snapshot contained:
                    //      "the quick brown fox jumps over the lazy dog"
                    // then the new snapshot will contain "the quick " and " fox jumps over the lazy dog", both of which are subspans on the original string.
                    // Having c._oldText -- "brown"-- represented as a subspan of the "the quick brown fox jumps over the lazy dog" doesn't pin anything that
                    // is not already pinned.
                    //
                    // Use +32 as a heuristic to reflect the cost of making a copy (if the total length of the insertions are close to the length of the
                    // original, there is no point in making a copy).
                    var totalInsertedCost = minimalChanges.Sum(c => c.NewLength + 32);
                    if (totalInsertedCost < change.NewLength)
                    {
                        // We have a scenario where the length of the inserted text is small compared to the original so we need to make copies
                        // of the inserted text to avoid pinning the original string.
                        for (int i = 0; (i < minimalChanges.Count); ++i)
                        {
                            var c = minimalChanges[i];
                            c._newText = c._newText.CreateCopy();
                        }
                    }

                    result.AddRange(minimalChanges);

                    // Sanity check
                    // If delta != 0, then we've constructed asymmetrical insertions and
                    // deletions, which should be impossible
                    Debug.Assert(delta == change.Delta, "Minimal edit delta should be equal to replaced text change's delta.");
                }
            }
            // If we aren't computing minimal changes, then copy over the non-null changes
            else
            {
                foreach (ITextChange change in work)
                {
                    if (change != null)
                    {
                        result.Add(change);
                    }
                }
            }

            return result;
        }

        private static IList<ITextChange> GetChangesFromDifferenceCollection(ref int delta,
                                                                            ITextChange originalChange,
                                                                            StringRebuilder oldText,
                                                                            StringRebuilder newText,
                                                                            IHierarchicalDifferenceCollection diffCollection,
                                                                            int leftOffset = 0,
                                                                            int rightOffset = 0)
        {
            List<ITextChange> changes = new List<ITextChange>();
            for (int i = 0; i < diffCollection.Differences.Count; i++)
            {
                Difference currentDiff = diffCollection.Differences[i];

                Span leftDiffSpan = Translate(diffCollection.LeftDecomposition.GetSpanInOriginal(currentDiff.Left), leftOffset);
                Span rightDiffSpan = Translate(diffCollection.RightDecomposition.GetSpanInOriginal(currentDiff.Right), rightOffset);

                IHierarchicalDifferenceCollection nextLevelDiffs = diffCollection.GetContainedDifferences(i);

                if (nextLevelDiffs != null)
                {
                    changes.AddRange(GetChangesFromDifferenceCollection(ref delta, originalChange, oldText, newText, nextLevelDiffs, leftDiffSpan.Start, rightDiffSpan.Start));
                }
                else
                {
                    ITextChange minimalChange = new ITextChange(originalChange.OldPosition + leftDiffSpan.Start,
                                                              oldText.GetSubText(leftDiffSpan),
                                                              newText.GetSubText(rightDiffSpan),
                                                              ComputeBoundaryConditions(originalChange, oldText, leftDiffSpan));

                    minimalChange.NewPosition = originalChange.NewPosition + rightDiffSpan.Start;
                    if (minimalChange.OldLength > 0 && minimalChange.NewLength > 0)
                    {
                        minimalChange.IsOpaque = true;
                    }

                    delta += minimalChange.Delta;
                    changes.Add(minimalChange);
                }
            }

            return changes;
        }


        private static LineBreakBoundaryConditions ComputeBoundaryConditions(ITextChange outerChange, StringRebuilder oldText, Span leftSpan)
        {
            LineBreakBoundaryConditions bc = LineBreakBoundaryConditions.None;
            if (leftSpan.Start == 0)
            {
                bc = (outerChange.LineBreakBoundaryConditions & LineBreakBoundaryConditions.PrecedingReturn);
            }
            else if (oldText[leftSpan.Start - 1] == '\r')
            {
                bc = LineBreakBoundaryConditions.PrecedingReturn;
            }
            if (leftSpan.End == oldText.Length)
            {
                bc |= (outerChange.LineBreakBoundaryConditions & LineBreakBoundaryConditions.SucceedingNewline);
            }
            else if (oldText[leftSpan.End] == '\n')
            {
                bc |= LineBreakBoundaryConditions.SucceedingNewline;
            }
            return bc;
        }

        private static Span Translate(Span span, int amount)
        {
            return new Span(span.Start + amount, span.Length);
        }

        private static bool PriorTo(ITextChange denormalizedChange, ITextChange normalizedChange, int accumulatedDelta, int accumulatedNormalizedDelta)
        {
            // notice that denormalizedChange.OldPosition == denormalizedChange.NewPosition
            if ((denormalizedChange.OldLength != 0) && (normalizedChange.OldLength != 0))
            {
                // both deletions
                return denormalizedChange.OldPosition <= normalizedChange.NewPosition - accumulatedDelta - accumulatedNormalizedDelta;
            }
            else
            {
                return denormalizedChange.OldPosition < normalizedChange.NewPosition - accumulatedDelta - accumulatedNormalizedDelta;
            }
        }

        /// <summary>
        /// Given a set of changes against a particular snapshot, merge in a list of normalized changes that occurred
        /// immediately after those changes (as part of the next snapshot) so that the merged changes refer to the 
        /// earlier snapshot.
        /// </summary>
        /// <param name="normalizedChanges">The list of changes to be merged.</param>
        /// <param name="denormChangesWithSentinel">The list of changes into which to merge.</param>
        public static void Denormalize(INormalizedTextChangeCollection normalizedChanges, List<ITextChange> denormChangesWithSentinel)
        {
            // denormalizedChangesWithSentinel contains a list of changes that have been denormalized to the origin snapshot
            // (the New positions in those changes are the same as the Old positions), and also has a sentinel at the end
            // that has int.MaxValue for its position.
            // args.Changes contains a list of changes that are normalized with respect to the most recent snapshot, so we know
            // that they are independent and properly ordered -- thus we can perform a single merge pass against the
            // denormalized changes.

            int rover = 0;
            int accumulatedDelta = 0;
            int accumulatedNormalizedDelta = 0;
            List<ITextChange> normChanges = new List<ITextChange>(normalizedChanges);
            for (int n = 0; n < normChanges.Count; ++n)
            {
                ITextChange normChange = normChanges[n];

                // 1. skip past all denormalized changes that begin prior to the beginning of the current change.

                while (PriorTo(denormChangesWithSentinel[rover], normChange, accumulatedDelta, accumulatedNormalizedDelta))
                {
                    accumulatedDelta += denormChangesWithSentinel[rover++].Delta;
                }

                // 2. normChange will be inserted at [rover], but it may need to be split

                if ((normChange.OldEnd - accumulatedDelta) > denormChangesWithSentinel[rover].OldPosition)
                {
                    // split required. for example, text at 5..10 was deleted in snapshot 1, and then text at 0..10 was deleted
                    // in snapshot 2; the latter turns into two deletions in terms of snapshot 1: 0..5 and 10..15.
                    int deletionSuffix = (normChange.OldEnd - accumulatedDelta) - denormChangesWithSentinel[rover].OldPosition;
                    int deletionPrefix = normChange.OldLength - deletionSuffix;
                    int normDelta = normChange.NewPosition - normChange.OldPosition;
                    denormChangesWithSentinel.Insert
                        (rover, new ITextChange(normChange.OldPosition - accumulatedDelta,
                                               ITextChange.ChangeOldSubText(normChange, 0, deletionPrefix),
                                               ITextChange.NewStringRebuilder(normChange),
                                               LineBreakBoundaryConditions.None, ITextChange.IsChangeOpaque(normChange)));
                    accumulatedNormalizedDelta += normDelta;

                    // the second part remains 'normalized' in case it needs to be split again
                    ITextChange splitee = new ITextChange(normChange.OldPosition + deletionPrefix,
                                                        ITextChange.ChangeOldSubText(normChange, deletionPrefix, deletionSuffix),
                                                        StringRebuilder.Empty,
                                                        LineBreakBoundaryConditions.None, ITextChange.IsChangeOpaque(normChange));
                    splitee.NewPosition += normDelta;
                    normChanges.Insert(n + 1, splitee);
                }
                else
                {
                    denormChangesWithSentinel.Insert
                        (rover, new ITextChange(normChange.OldPosition - accumulatedDelta,
                                               ITextChange.OldStringRebuilder(normChange),
                                               ITextChange.NewStringRebuilder(normChange),
                                               LineBreakBoundaryConditions.None, ITextChange.IsChangeOpaque(normChange)));
                    accumulatedNormalizedDelta += normChange.Delta;
                }

                rover++;
            }
        }

        int ICollection<ITextChange>.Count => _changes.Count;

        bool ICollection<ITextChange>.IsReadOnly => true;

        ITextChange IList<ITextChange>.this[int index]
        {
            get => _changes[index];
            set => throw new NotSupportedException();
        }


        int IList<ITextChange>.IndexOf(ITextChange item)
        {
            for (int i = 0; (i < _changes.Count); ++i)
            {
                if (item.Equals(_changes[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        void IList<ITextChange>.Insert(int index, ITextChange item)
        {
            throw new NotSupportedException();
        }

        void IList<ITextChange>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        void ICollection<ITextChange>.Add(ITextChange item)
        {
            throw new NotSupportedException();
        }

        void ICollection<ITextChange>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<ITextChange>.Contains(ITextChange item)
        {
            return ((IList<ITextChange>)this).IndexOf(item) != -1;
        }

        void ICollection<ITextChange>.CopyTo(ITextChange[] array, int arrayIndex)
        {
            for (int i = 0; (i < _changes.Count); ++i)
            {
                array[i + arrayIndex] = _changes[i];
            }
        }

        bool ICollection<ITextChange>.Remove(ITextChange item)
        {
            throw new NotSupportedException();
        }

        IEnumerator<ITextChange> IEnumerable<ITextChange>.GetEnumerator()
        {
            return _changes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _changes.GetEnumerator();
        }
    }
}
