// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities
{
    internal static class TextChangeRangeExtensions
    {
        public static TextChangeRange? Accumulate(this TextChangeRange? accumulatedTextChangeSoFar, IEnumerable<TextChangeRange> changesInNextVersion)
        {
            if (!changesInNextVersion.Any())
            {
                return accumulatedTextChangeSoFar;
            }

            // get encompassing text change and accumulate it once. 
            // we could apply each one individually like we do in SyntaxDiff::ComputeSpansInNew by calculating delta
            // between each change in changesInNextVersion which is already sorted in its textual position ascending order.
            // but end result will be same as just applying it once with encompassed text change range.
            var newChange = TextChangeRange.Collapse(changesInNextVersion);

            // no previous accumulated change, return the new value.
            if (accumulatedTextChangeSoFar == null)
            {
                return newChange;
            }

            // set initial value from the old one.
            var currentStart = accumulatedTextChangeSoFar.Value.Span.Start;
            var currentOldEnd = accumulatedTextChangeSoFar.Value.Span.End;
            var currentNewEnd = accumulatedTextChangeSoFar.Value.Span.Start + accumulatedTextChangeSoFar.Value.NewLength;

            // this is a port from 
            //      csharp\rad\Text\SourceText.cpp - CSourceText::OnChangeLineText
            // which accumulate text changes to one big text change that would encompass all changes

            // Merge incoming edit data with old edit data here.
            // RULES:
            // 1) position values are always associated with a buffer version.
            // 2) Comparison between position values is only allowed if their
            //    buffer version is the same.
            // 3) newChange.Span.End and newChange.Span.Start + newChange.NewLength (both stored and incoming) 
            //    refer to the same position, but have different buffer versions.
            // 4) The incoming end position is associated with buffer versions
            //    n-1 (old) and n(new).
            // 5) The stored end position BEFORE THIS EDIT is associated with
            //    buffer versions 0 (old) and n-1 (new).
            // 6) The stored end position AFTER THIS EDIT should be associated
            //    with buffer versions 0 (old) and n(new).
            // 7) To transform a position P from buffer version of x to y, apply
            //    the delta between any position C(x) and C(y), ASSUMING that
            //    both positions P and C are affected by all edits between
            //    buffer versions x and y.
            // 8) The start position is relative to all buffer versions, because
            //    it precedes all edits(by definition)

            // First, the start position.  This one is easy, because it is not
            // complicated by buffer versioning -- it is always the "earliest"
            // of all incoming values.
            if (newChange.Span.Start < currentStart)
            {
                currentStart = newChange.Span.Start;
            }

            // Okay, now the end position.  We must make a choice between the
            // stored end position and the incoming end position.  Per rule #2,
            // we must use the stored NEW end and the incoming OLD end, both of
            // which are relative to buffer version n-1.
            if (currentNewEnd > newChange.Span.End)
            {
                // We have chosen to keep the stored end because it occurs past
                // the incoming edit.  So, we need currentOldEnd and
                // currentNewEnd.  Since currentOldEnd is already relative
                // to buffer 0, it is unmodified.  Since currentNewEnd is
                // relative to buffer n-1 (and we need n), apply to it the delta
                // between the incoming end position values, which are n-1 and n.
                currentNewEnd = currentNewEnd + newChange.NewLength - newChange.Span.Length;
            }
            else
            {
                // We have chosen to use the incoming end because it occurs past
                // the stored edit.  So, we need newChange.Span.End and (newChange.Span.Start + newChange.NewLength).
                // Since (newChange.Span.Start + newChange.NewLength) is already relative to buffer n, it is copied
                // unmodified.  Since newChange.Span.End is relative to buffer n-1 (and
                // we need 0), apply to it the delta between the stored end
                // position values, which are relative to 0 and n-1.
                currentOldEnd = currentOldEnd + newChange.Span.End - currentNewEnd;
                currentNewEnd = newChange.Span.Start + newChange.NewLength;
            }

            return new TextChangeRange(TextSpan.FromBounds(currentStart, currentOldEnd), currentNewEnd - currentStart);
        }

        public static TextChangeRange ToTextChangeRange(this TextChange textChange)
        {
            return new TextChangeRange(textChange.Span, textChange.NewText?.Length ?? 0);
        }

        /// <summary>
        /// Merges the new change ranges into the old change ranges, adjusting the new ranges to be with respect to the original text
        /// (with neither old or new changes applied) instead of with respect to the original text after "old changes" are applied.
        ///
        /// This may require splitting, concatenation, etc. of individual change ranges.
        /// </summary>
        /// <remarks>
        /// Both `oldChanges` and `newChanges` must contain non-overlapping spans in ascending order.
        /// </remarks>
        public static ImmutableArray<TextChangeRange> Merge(ImmutableArray<TextChangeRange> oldChanges, ImmutableArray<TextChangeRange> newChanges)
        {
            // Earlier steps are expected to prevent us from ever reaching this point with empty change sets.
            if (oldChanges.IsEmpty)
            {
                throw new ArgumentException(nameof(oldChanges));
            }

            if (newChanges.IsEmpty)
            {
                throw new ArgumentException(nameof(newChanges));
            }

            var builder = ArrayBuilder<TextChangeRange>.GetInstance();

            var oldChange = oldChanges[0];
            var newChange = new UnadjustedNewChange(newChanges[0]);

            var oldIndex = 0;
            var newIndex = 0;

            // The sum of characters inserted by old changes minus characters deleted by old changes.
            // This value must be adjusted whenever characters from an old change are added to `builder`.
            var oldDelta = 0;

            // In this loop we "zip" together potentially overlapping old and new changes.
            // It's important that when overlapping changes are found, we don't consume past the end of the overlapping section until the next iteration.
            // so that we don't miss scenarios where the section after the overlap we found itself overlaps with another change
            // e.g.:
            // [-------oldChange1------]
            // [--newChange1--]   [--newChange2--]
            while (true)
            {
                if (oldChange.Span.Length == 0 && oldChange.NewLength == 0)
                {
                    // old change does not insert or delete any characters, so it can be dropped to no effect.
                    if (tryGetNextOldChange()) continue;
                    else break;
                }
                else if (newChange.SpanLength == 0 && newChange.NewLength == 0)
                {
                    // new change does not insert or delete any characters, so it can be dropped to no effect.
                    if (tryGetNextNewChange()) continue;
                    else break;
                }
                else if (newChange.SpanEnd <= oldChange.Span.Start + oldDelta)
                {
                    // new change is entirely before old change, so just take the new change
                    //                old[--------]
                    // new[--------]
                    adjustAndAddNewChange(builder, oldDelta, newChange);
                    if (tryGetNextNewChange()) continue;
                    else break;
                }
                else if (newChange.SpanStart >= oldChange.NewEnd() + oldDelta)
                {
                    // new change is entirely after old change, so just take the old change
                    // old[--------]
                    //                new[--------]
                    addAndAdjustOldDelta(builder, ref oldDelta, oldChange);
                    if (tryGetNextOldChange()) continue;
                    else break;
                }
                else if (newChange.SpanStart < oldChange.Span.Start + oldDelta)
                {
                    // new change starts before old change, but the new change deletion overlaps with the old change insertion
                    // note: 'd' represents a deleted character, 'a' represents a character inserted by an old change, and 'b' represents a character inserted by a new change.
                    //
                    //    old|dddddd|
                    //       |aaaaaa|
                    // ---------------
                    // new|dddddd|
                    //    |bbbbbb|

                    // align the new change and old change start by consuming the part of the new deletion before the old change
                    // (this only deletes characters of the original text)
                    //
                    // old|dddddd|
                    //    |aaaaaa|
                    // ---------------
                    // new|ddd|
                    //    |bbbbbb|
                    var newChangeLeadingDeletion = oldChange.Span.Start + oldDelta - newChange.SpanStart;
                    adjustAndAddNewChange(builder, oldDelta, new UnadjustedNewChange(newChange.SpanStart, newChangeLeadingDeletion, newLength: 0));
                    newChange = new UnadjustedNewChange(oldChange.Span.Start + oldDelta, newChange.SpanLength - newChangeLeadingDeletion, newChange.NewLength);
                    continue;
                }
                else if (newChange.SpanStart > oldChange.Span.Start + oldDelta)
                {
                    // new change starts after old change, but overlaps
                    //
                    // old|dddddd|
                    //    |aaaaaa|
                    // ---------------
                    //    new|dddddd|
                    //       |bbbbbb|

                    // align the old change to the new change by consuming the part of the old change which is before the new change.
                    //
                    //    old|ddd|
                    //       |aaa|
                    // ---------------
                    //    new|dddddd|
                    //       |bbbbbb|

                    var oldChangeLeadingInsertion = newChange.SpanStart - (oldChange.Span.Start + oldDelta);
                    // we must make sure to delete at most as many characters as the entire oldChange deletes
                    var oldChangeLeadingDeletion = Math.Min(oldChange.Span.Length, oldChangeLeadingInsertion);
                    addAndAdjustOldDelta(builder, ref oldDelta, new TextChangeRange(new TextSpan(oldChange.Span.Start, oldChangeLeadingDeletion), oldChangeLeadingInsertion));
                    oldChange = new TextChangeRange(new TextSpan(newChange.SpanStart - oldDelta, oldChange.Span.Length - oldChangeLeadingDeletion), oldChange.NewLength - oldChangeLeadingInsertion);
                    continue;
                }
                else
                {
                    // old and new change start at same adjusted position
                    Debug.Assert(newChange.SpanStart == oldChange.Span.Start + oldDelta);

                    if (newChange.SpanLength <= oldChange.NewLength)
                    {
                        // new change deletes fewer characters than old change inserted
                        //
                        // old|dddddd|
                        //    |aaaaaa|
                        // ---------------
                        // new|ddd|
                        //    |bbbbbb|

                        // - apply the new change deletion to the old change insertion
                        //
                        //    old|dddddd|
                        //       |aaa|
                        // ---------------
                        // new||
                        //    |bbbbbb|
                        //
                        // - move the new change insertion forward by the same amount as its consumed deletion to remain aligned with the old change.
                        // (because the old change and new change have the same adjusted start position, the new change insertion appears directly before the old change insertion in the final text)
                        //
                        //    old|dddddd|
                        //       |aaa|
                        // ---------------
                        //    new||
                        //       |bbbbbb|

                        oldChange = new TextChangeRange(oldChange.Span, oldChange.NewLength - newChange.SpanLength);

                        // the new change deletion is equal to the subset of the old change insertion that we are consuming this iteration
                        oldDelta = oldDelta + newChange.SpanLength;

                        // since the new change insertion occurs before the old change, consume it now
                        newChange = new UnadjustedNewChange(newChange.SpanEnd, spanLength: 0, newChange.NewLength);
                        adjustAndAddNewChange(builder, oldDelta, newChange);
                        if (tryGetNextNewChange()) continue;
                        else break;
                    }
                    else
                    {
                        // new change deletes more characters than old change inserted
                        //
                        // old|d|
                        //    |aa|
                        // ---------------
                        // new|ddd|
                        //    |bbb|

                        // merge the old change into the new change:
                        // - new change deletion deletes all of the old change insertion. reduce the new change deletion accordingly
                        //
                        //   old|d|
                        //      ||
                        // ---------------
                        // new|d|
                        //    |bbb|
                        //
                        // - old change deletion is simply added to the new change deletion.
                        //
                        //  old||
                        //     ||
                        // ---------------
                        // new|dd|
                        //    |bbb|
                        //
                        // - new change is moved to put its adjusted position equal to the old change we just merged in
                        //
                        //  old||
                        //     ||
                        // ---------------
                        //  new|dd|
                        //     |bbb|

                        // adjust the oldDelta to reflect that the old change has been consumed
                        oldDelta = oldDelta - oldChange.Span.Length + oldChange.NewLength;

                        var newDeletion = newChange.SpanLength + oldChange.Span.Length - oldChange.NewLength;
                        newChange = new UnadjustedNewChange(oldChange.Span.Start + oldDelta, newDeletion, newChange.NewLength);
                        if (tryGetNextOldChange()) continue;
                        else break;
                    }
                }
            }

            // there may be remaining old changes or remaining new changes (not both, and not neither)
            switch (oldIndex == oldChanges.Length, newIndex == newChanges.Length)
            {
                case (true, true):
                case (false, false):
                    throw new InvalidOperationException();
            }

            while (oldIndex < oldChanges.Length)
            {
                addAndAdjustOldDelta(builder, ref oldDelta, oldChange);
                tryGetNextOldChange();
            }

            while (newIndex < newChanges.Length)
            {
                adjustAndAddNewChange(builder, oldDelta, newChange);
                tryGetNextNewChange();
            }

            return builder.ToImmutableAndFree();

            bool tryGetNextOldChange()
            {
                oldIndex++;
                if (oldIndex < oldChanges.Length)
                {
                    oldChange = oldChanges[oldIndex];
                    return true;
                }
                else
                {
                    oldChange = default;
                    return false;
                }
            }

            bool tryGetNextNewChange()
            {
                newIndex++;
                if (newIndex < newChanges.Length)
                {
                    newChange = new UnadjustedNewChange(newChanges[newIndex]);
                    return true;
                }
                else
                {
                    newChange = default;
                    return false;
                }
            }

            static void addAndAdjustOldDelta(ArrayBuilder<TextChangeRange> builder, ref int oldDelta, TextChangeRange oldChange)
            {
                // modify oldDelta to reflect characters deleted and inserted by an old change
                oldDelta = oldDelta - oldChange.Span.Length + oldChange.NewLength;
                add(builder, oldChange);
            }

            static void adjustAndAddNewChange(ArrayBuilder<TextChangeRange> builder, int oldDelta, UnadjustedNewChange newChange)
            {
                // unadjusted new change is relative to the original text with old changes applied. Subtract oldDelta to make it relative to the original text.
                add(builder, new TextChangeRange(new TextSpan(newChange.SpanStart - oldDelta, newChange.SpanLength), newChange.NewLength));
            }

            static void add(ArrayBuilder<TextChangeRange> builder, TextChangeRange change)
            {
                if (builder.Count > 0)
                {
                    var last = builder[^1];
                    if (last.Span.End == change.Span.Start)
                    {
                        // merge changes together if they are adjacent
                        builder[^1] = new TextChangeRange(new TextSpan(last.Span.Start, last.Span.Length + change.Span.Length), last.NewLength + change.NewLength);
                        return;
                    }
                    else if (last.Span.End > change.Span.Start)
                    {
                        throw new ArgumentOutOfRangeException(nameof(change));
                    }

                }

                builder.Add(change);
            }
        }

        /// <summary>
        /// Represents a new change being processed by <see cref="Merge(ImmutableArray&lt;TextChangeRange&gt;, ImmutableArray&lt;TextChangeRange&gt;)"/>.
        /// Such a new change must be adjusted before being added to the result list.
        /// </summary>
        /// <remarks>
        /// A value of this type may represent the intermediate state of merging of an old change into an unadjusted new change,
        /// resulting in a temporary unadjusted new change whose <see cref="SpanStart"/> is negative (not valid) until it is adjusted.
        /// This tends to happen when we need to merge an old change deletion into a new change near the beginning of the text. (see TextChangeTests.Fuzz_4)
        /// </remarks>
        private readonly struct UnadjustedNewChange
        {
            public readonly int SpanStart { get; }
            public readonly int SpanLength { get; }
            public readonly int NewLength { get; }

            public int SpanEnd => SpanStart + SpanLength;

            public UnadjustedNewChange(int spanStart, int spanLength, int newLength)
            {
                SpanStart = spanStart;
                SpanLength = spanLength;
                NewLength = newLength;
            }

            public UnadjustedNewChange(TextChangeRange range)
                : this(range.Span.Start, range.Span.Length, range.NewLength)
            {
            }
        }

        private static int NewEnd(this TextChangeRange range) => range.Span.Start + range.NewLength;
    }
}
