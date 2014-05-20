// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Microsoft.CodeAnalysis.Text
{
    internal sealed class ChangedText : SourceText
    {
        private readonly SourceText oldText;
        private readonly SourceText newText;
        private readonly ImmutableArray<TextChangeRange> changes;

        public ChangedText(SourceText oldText, ImmutableArray<TextChangeRange> changeRanges, ImmutableArray<SourceText> segments)
        {
            Debug.Assert(oldText != null);
            Debug.Assert(!changeRanges.IsDefault);
            Debug.Assert(!segments.IsDefault);

            this.oldText = oldText;
            this.newText = segments.IsEmpty ? new StringText("", oldText.Encoding) : (SourceText)new CompositeText(segments);
            this.changes = changeRanges;
        }

        public override Encoding Encoding
        {
            get { return oldText.Encoding; }
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

            if (ReferenceEquals(this.oldText, oldText))
            {
                // check whether the bases are same one
                return this.changes;
            }

            if (this.oldText.GetChangeRanges(oldText).Count == 0)
            {
                // okay, the bases are different, but the contents might be same.
                return this.changes;
            }

            if (this == oldText)
            {
                return TextChangeRange.NoChanges;
            }

            return ImmutableArray.Create(new TextChangeRange(new TextSpan(0, oldText.Length), newText.Length));
        }
    }
}