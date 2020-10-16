// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    internal sealed class ChangedText : SourceText
    {
        private readonly SourceText _newText;
        private readonly ChangeInfo _info;

        public ChangedText(SourceText oldText, SourceText newText, ImmutableArray<TextChangeRange> changeRanges)
            : base(checksumAlgorithm: oldText.ChecksumAlgorithm)
        {
            RoslynDebug.Assert(newText != null);
            Debug.Assert(newText is CompositeText || newText is SubText || newText is StringText || newText is LargeText);
            RoslynDebug.Assert(oldText != null);
            Debug.Assert(oldText != newText);
            Debug.Assert(!changeRanges.IsDefault);
            RequiresChangeRangesAreValid(oldText, newText, changeRanges);

            _newText = newText;
            _info = new ChangeInfo(changeRanges, new WeakReference<SourceText>(oldText), (oldText as ChangedText)?._info);
        }

        private static void RequiresChangeRangesAreValid(
            SourceText oldText, SourceText newText, ImmutableArray<TextChangeRange> changeRanges)
        {
            var deltaLength = 0;
            foreach (var change in changeRanges)
                deltaLength += change.NewLength - change.Span.Length;

            if (oldText.Length + deltaLength != newText.Length)
                throw new InvalidOperationException("Delta length difference of change ranges didn't match before/after text length.");

            var position = 0;
            foreach (var change in changeRanges)
            {
                if (change.Span.Start < position)
                    throw new InvalidOperationException("Change preceded current position in oldText");

                if (change.Span.Start > oldText.Length)
                    throw new InvalidOperationException("Change start was after the end of oldText");

                if (change.Span.End > oldText.Length)
                    throw new InvalidOperationException("Change end was after the end of oldText");

                position = change.Span.End;
            }
        }

        private class ChangeInfo
        {
            public ImmutableArray<TextChangeRange> ChangeRanges { get; }

            // store old text weakly so we don't form unwanted chains of old texts (especially chains of ChangedTexts)
            // used to identify the changes in GetChangeRanges.
            public WeakReference<SourceText> WeakOldText { get; }

            public ChangeInfo? Previous { get; private set; }

            public ChangeInfo(ImmutableArray<TextChangeRange> changeRanges, WeakReference<SourceText> weakOldText, ChangeInfo? previous)
            {
                this.ChangeRanges = changeRanges;
                this.WeakOldText = weakOldText;
                this.Previous = previous;
                Clean();
            }

            // clean up 
            private void Clean()
            {
                // look for last info in the chain that still has reference to old text
                ChangeInfo? lastInfo = this;
                for (ChangeInfo? info = this; info != null; info = info.Previous)
                {
                    SourceText? tmp;
                    if (info.WeakOldText.TryGetTarget(out tmp))
                    {
                        lastInfo = info;
                    }
                }

                // break chain for any info's beyond that so they get GC'd
                ChangeInfo? prev;
                while (lastInfo != null)
                {
                    prev = lastInfo.Previous;
                    lastInfo.Previous = null;
                    lastInfo = prev;
                }
            }
        }

        public override Encoding? Encoding
        {
            get { return _newText.Encoding; }
        }

        public IEnumerable<TextChangeRange> Changes
        {
            get { return _info.ChangeRanges; }
        }

        public override int Length
        {
            get { return _newText.Length; }
        }

        internal override int StorageSize
        {
            get { return _newText.StorageSize; }
        }

        internal override ImmutableArray<SourceText> Segments
        {
            get { return _newText.Segments; }
        }

        internal override SourceText StorageKey
        {
            get { return _newText.StorageKey; }
        }

        public override char this[int position]
        {
            get { return _newText[position]; }
        }

        public override string ToString(TextSpan span)
        {
            return _newText.ToString(span);
        }

        public override SourceText GetSubText(TextSpan span)
        {
            return _newText.GetSubText(span);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            _newText.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public override SourceText WithChanges(IEnumerable<TextChange> changes)
        {
            // compute changes against newText to avoid capturing strong references to this ChangedText instance.
            // _newText will only ever be one of CompositeText, SubText, StringText or LargeText, so calling WithChanges on it 
            // will either produce a ChangeText instance or the original instance in case of a empty change.
            var changed = _newText.WithChanges(changes) as ChangedText;
            if (changed != null)
            {
                return new ChangedText(this, changed._newText, changed._info.ChangeRanges);
            }
            else
            {
                // change was empty, so just return this same instance
                return this;
            }
        }

        public override IReadOnlyList<TextChangeRange> GetChangeRanges(SourceText oldText)
        {
            if (oldText == null)
            {
                throw new ArgumentNullException(nameof(oldText));
            }

            if (this == oldText)
            {
                return TextChangeRange.NoChanges;
            }

            // try this quick check first
            SourceText? actualOldText;
            if (_info.WeakOldText.TryGetTarget(out actualOldText) && actualOldText == oldText)
            {
                // the supplied old text is the one we directly reference, so the changes must be the ones we have.
                return _info.ChangeRanges;
            }

            // otherwise look to see if there are a series of changes from the old text to this text and merge them.
            if (IsChangedFrom(oldText))
            {
                var changes = GetChangesBetween(oldText, this);
                if (changes.Count > 1)
                {
                    return Merge(changes);
                }
            }

            // the SourceText subtype for editor snapshots knows when two snapshots from the same buffer have the same contents
            if (actualOldText != null && actualOldText.GetChangeRanges(oldText).Count == 0)
            {
                // the texts are different instances, but the contents are considered to be the same.
                return _info.ChangeRanges;
            }

            return ImmutableArray.Create(new TextChangeRange(new TextSpan(0, oldText.Length), _newText.Length));
        }

        private bool IsChangedFrom(SourceText oldText)
        {
            for (ChangeInfo? info = _info; info != null; info = info.Previous)
            {
                SourceText? text;
                if (info.WeakOldText.TryGetTarget(out text) && text == oldText)
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<ImmutableArray<TextChangeRange>> GetChangesBetween(SourceText oldText, ChangedText newText)
        {
            var list = new List<ImmutableArray<TextChangeRange>>();

            ChangeInfo? change = newText._info;
            list.Add(change.ChangeRanges);

            while (change != null)
            {
                SourceText? actualOldText;
                change.WeakOldText.TryGetTarget(out actualOldText);

                if (actualOldText == oldText)
                {
                    return list;
                }

                change = change.Previous;
                if (change != null)
                {
                    list.Insert(0, change.ChangeRanges);
                }
            }

            // did not find old text, so not connected?
            list.Clear();
            return list;
        }

        private static ImmutableArray<TextChangeRange> Merge(IReadOnlyList<ImmutableArray<TextChangeRange>> changeSets)
        {
            Debug.Assert(changeSets.Count > 1);

            var merged = changeSets[0];
            for (int i = 1; i < changeSets.Count; i++)
            {
                merged = Merge(merged, changeSets[i]);
            }

            return merged;
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

        /// <summary>
        /// Merges the new change ranges into the old change ranges, adjusting the new ranges to be with respect to the original text
        /// (with neither old or new changes applied) instead of with respect to the original text after "old changes" are applied.
        ///
        /// This may require splitting, concatenation, etc. of individual change ranges.
        /// </summary>
        /// <remarks>
        /// Both `oldChanges` and `newChanges` must contain non-overlapping spans in ascending order.
        /// </remarks>
        private static ImmutableArray<TextChangeRange> Merge(ImmutableArray<TextChangeRange> oldChanges, ImmutableArray<TextChangeRange> newChanges)
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
                else if (newChange.SpanStart >= oldChange.NewEnd + oldDelta)
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
        /// Computes line starts faster given already computed line starts from text before the change.
        /// </summary>
        protected override TextLineCollection GetLinesCore()
        {
            SourceText? oldText;
            TextLineCollection? oldLineInfo;

            if (!_info.WeakOldText.TryGetTarget(out oldText) || !oldText.TryGetLines(out oldLineInfo))
            {
                // no old line starts? do it the hard way.
                return base.GetLinesCore();
            }

            // compute line starts given changes and line starts already computed from previous text
            var lineStarts = ArrayBuilder<int>.GetInstance();
            lineStarts.Add(0);

            // position in the original document
            var position = 0;

            // delta generated by already processed changes (position in the new document = position + delta)
            var delta = 0;

            // true if last segment ends with CR and we need to check for CR+LF code below assumes that both CR and LF are also line breaks alone
            var endsWithCR = false;

            foreach (var change in _info.ChangeRanges)
            {
                // include existing line starts that occur before this change
                if (change.Span.Start > position)
                {
                    if (endsWithCR && _newText[position + delta] == '\n')
                    {
                        // remove last added line start (it was due to previous CR)
                        // a new line start including the LF will be added next
                        lineStarts.RemoveLast();
                    }

                    var lps = oldLineInfo.GetLinePositionSpan(TextSpan.FromBounds(position, change.Span.Start));
                    for (int i = lps.Start.Line + 1; i <= lps.End.Line; i++)
                    {
                        lineStarts.Add(oldLineInfo[i].Start + delta);
                    }

                    endsWithCR = oldText[change.Span.Start - 1] == '\r';

                    // in case change is inserted between CR+LF we treat CR as line break alone, 
                    // but this line break might be retracted and replaced with new one in case LF is inserted  
                    if (endsWithCR && change.Span.Start < oldText.Length && oldText[change.Span.Start] == '\n')
                    {
                        lineStarts.Add(change.Span.Start + delta);
                    }
                }

                // include line starts that occur within newly inserted text
                if (change.NewLength > 0)
                {
                    var changeStart = change.Span.Start + delta;
                    var text = GetSubText(new TextSpan(changeStart, change.NewLength));

                    if (endsWithCR && text[0] == '\n')
                    {
                        // remove last added line start (it was due to previous CR)
                        // a new line start including the LF will be added next
                        lineStarts.RemoveLast();
                    }

                    // Skip first line (it is always at offset 0 and corresponds to the previous line)
                    for (int i = 1; i < text.Lines.Count; i++)
                    {
                        lineStarts.Add(changeStart + text.Lines[i].Start);
                    }

                    endsWithCR = text[change.NewLength - 1] == '\r';
                }

                position = change.Span.End;
                delta += (change.NewLength - change.Span.Length);
            }

            // include existing line starts that occur after all changes
            if (position < oldText.Length)
            {
                if (endsWithCR && _newText[position + delta] == '\n')
                {
                    // remove last added line start (it was due to previous CR)
                    // a new line start including the LF will be added next
                    lineStarts.RemoveLast();
                }

                var lps = oldLineInfo.GetLinePositionSpan(TextSpan.FromBounds(position, oldText.Length));
                for (int i = lps.Start.Line + 1; i <= lps.End.Line; i++)
                {
                    lineStarts.Add(oldLineInfo[i].Start + delta);
                }
            }

            return new LineInfo(this, lineStarts.ToArrayAndFree());
        }

        internal static class TestAccessor
        {
            public static ImmutableArray<TextChangeRange> Merge(ImmutableArray<TextChangeRange> oldChanges, ImmutableArray<TextChangeRange> newChanges)
                => ChangedText.Merge(oldChanges, newChanges);
        }
    }
}
