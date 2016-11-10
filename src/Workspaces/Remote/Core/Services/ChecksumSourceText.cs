// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// source text that has checksum tied to it
    /// </summary>
    internal class ChecksumSourceText : SourceText
    {
        private readonly SourceText _sourceText;

        public ChecksumSourceText(Checksum checksum, SourceText sourceText)
        {
            Checksum = checksum;
            _sourceText = sourceText;
        }

        public Checksum Checksum { get; }

        public override char this[int position] => _sourceText[position];
        public override Encoding Encoding => _sourceText.Encoding;
        public override int Length => _sourceText.Length;
        public override SourceTextContainer Container => _sourceText.Container;

        public override SourceText WithChanges(IEnumerable<TextChange> changes) => _sourceText.WithChanges(changes);
        public override SourceText GetSubText(TextSpan span) => _sourceText.GetSubText(span);
        public override IReadOnlyList<TextChange> GetTextChanges(SourceText oldText) => _sourceText.GetTextChanges(oldText);
        public override IReadOnlyList<TextChangeRange> GetChangeRanges(SourceText oldText) => _sourceText.GetChangeRanges(oldText);

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) =>
            _sourceText.CopyTo(sourceIndex, destination, destinationIndex, count);
        public override void Write(TextWriter writer, TextSpan span, CancellationToken cancellationToken = default(CancellationToken)) =>
            _sourceText.Write(writer, span, cancellationToken);

        public override bool Equals(object obj) => _sourceText.Equals(obj);
        public override int GetHashCode() => _sourceText.GetHashCode();

        public override string ToString() => _sourceText.ToString();
        public override string ToString(TextSpan span) => _sourceText.ToString(span);

        protected override TextLineCollection GetLinesCore() => _sourceText.Lines;

        protected override bool ContentEqualsImpl(SourceText other)
        {
            var otherChecksum = other as ChecksumSourceText;
            if (otherChecksum != null)
            {
                return Checksum == otherChecksum.Checksum;
            }

            return _sourceText.ContentEquals(other);
        }
    }
}
