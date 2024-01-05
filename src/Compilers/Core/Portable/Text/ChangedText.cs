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
                merged = TextChangeRangeExtensions.Merge(merged, changeSets[i]);
            }

            return merged;
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
            var lineStarts = ArrayBuilder<int>.GetInstance(oldLineInfo.Count);
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
                => TextChangeRangeExtensions.Merge(oldChanges, newChanges);
        }
    }
}
