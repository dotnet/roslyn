// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Text
{
    internal class ChangedText : SourceText
    {
        private readonly SourceText oldText;
        private readonly SourceText newText;
        private readonly ImmutableArray<TextChangeRange> changes;

        public ChangedText(SourceText oldText, IEnumerable<TextChange> changes)
        {
            if (oldText == null)
            {
                throw new ArgumentNullException("text");
            }

            if (changes == null)
            {
                throw new ArgumentNullException("changes");
            }

            var segments = ArrayBuilder<SourceText>.GetInstance();
            var changeRanges = ArrayBuilder<TextChangeRange>.GetInstance();
            int position = 0;

            foreach (var change in changes)
            {
                // there can be no overlapping changes
                if (change.Span.Start < position)
                {
                    throw new InvalidOperationException("The changes must be ordered and not overlapping.");
                }

                // if we've skipped a range, add
                if (change.Span.Start > position)
                {
                    var subText = oldText.GetSubText(new TextSpan(position, change.Span.Start - position));
                    CompositeText.AddSegments(segments, subText);
                }

                if (!string.IsNullOrEmpty(change.NewText))
                {
                    var segment = SourceText.From(change.NewText);
                    CompositeText.AddSegments(segments, segment);
                }

                position = change.Span.End;

                changeRanges.Add(new TextChangeRange(change.Span, change.NewText != null ? change.NewText.Length : 0));
            }

            if (position < oldText.Length)
            {
                var subText = oldText.GetSubText(new TextSpan(position, oldText.Length - position));
                CompositeText.AddSegments(segments, subText);
            }

            this.oldText = oldText;
            this.newText = new CompositeText(segments.ToImmutableAndFree());
            this.changes = changeRanges.ToImmutableAndFree();
        }

        public SourceText OldText
        {
            get { return this.oldText; }
        }

        public SourceText NewText
        {
            get { return this.newText; }
        }

        public IEnumerable<TextChangeRange> Changes
        {
            get { return this.changes; }
        }

        public override int Length
        {
            get { return this.newText.Length; }
        }

        public override char this[int position]
        {
            get { return this.newText[position]; }
        }

        public override string ToString(TextSpan span)
        {
            return this.newText.ToString(span);
        }

        public override SourceText GetSubText(TextSpan span)
        {
            return this.newText.GetSubText(span);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            this.newText.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public override IReadOnlyList<TextChangeRange> GetChangeRanges(SourceText oldText)
        {
            if (oldText == null)
            {
                throw new ArgumentNullException("oldText");
            }

            if (this.oldText == oldText)
            {
                // check whether the bases are same one
                return this.changes;
            }
            else if (this.oldText.GetChangeRanges(oldText).Count == 0)
            {
                // okay, the bases are different, but the contents might be same.
                return this.changes;
            }
            else if (this == oldText)
            {
                return TextChangeRange.NoChanges;
            }
            else
            {
                return ImmutableList.Create(new TextChangeRange(new TextSpan(0, oldText.Length), newText.Length));
            }
        }
    }
}