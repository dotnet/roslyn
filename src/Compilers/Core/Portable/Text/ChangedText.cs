// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Microsoft.CodeAnalysis.Text
{
    internal sealed class ChangedText : SourceText
    {
        private readonly SourceText _newText;

        // store old text weakly so we don't form unwanted chains of old texts (especially chains of ChangedTexts)
        // It is only used to identify the old text in GetChangeRanges which only returns the changes if old text matches identity.
        private readonly WeakReference<SourceText> _weakOldText;
        private readonly ImmutableArray<TextChangeRange> _changes;

        public ChangedText(SourceText oldText, SourceText newText, ImmutableArray<TextChangeRange> changeRanges)
            : base(checksumAlgorithm: oldText.ChecksumAlgorithm)
        {
            Debug.Assert(newText != null);
            Debug.Assert(oldText != null);
            Debug.Assert(oldText != newText);
            Debug.Assert(!changeRanges.IsDefault);

            _newText = newText;
            _weakOldText = new WeakReference<SourceText>(oldText);
            _changes = changeRanges;
        }

        public override Encoding Encoding
        {
            get { return _newText.Encoding; }
        }

        public IEnumerable<TextChangeRange> Changes
        {
            get { return _changes; }
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
            var changed = (ChangedText)_newText.WithChanges(changes);
            return new ChangedText(this, changed._newText, changed._changes);
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

            SourceText actualOldText;
            if (_weakOldText.TryGetTarget(out actualOldText))
            {
                if (actualOldText == oldText)
                {
                    // same identity, so the changes must be the ones we have.
                    return _changes;
                }

                if (actualOldText.GetChangeRanges(oldText).Count == 0)
                {
                    // the bases are different instances, but the contents are considered to be the same.
                    return _changes;
                }
            }

            return ImmutableArray.Create(new TextChangeRange(new TextSpan(0, oldText.Length), _newText.Length));
        }
    }
}
