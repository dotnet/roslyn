// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// A <see cref="SourceText"/> that represents a subrange of another <see cref="SourceText"/>.
    /// </summary>
    internal sealed class SubText : SourceText
    {
        public SubText(SourceText text, TextSpan span)
            : base(checksumAlgorithm: text.ChecksumAlgorithm)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (span.Start < 0
                || span.Start >= text.Length
                || span.End < 0
                || span.End > text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            UnderlyingText = text;
            UnderlyingSpan = span;
        }

        public override Encoding? Encoding => UnderlyingText.Encoding;

        public SourceText UnderlyingText { get; }

        public TextSpan UnderlyingSpan { get; }

        public override int Length => UnderlyingSpan.Length;

        internal override int StorageSize
        {
            get { return this.UnderlyingText.StorageSize; }
        }

        internal override SourceText StorageKey
        {
            get { return this.UnderlyingText.StorageKey; }
        }

        public override char this[int position]
        {
            get
            {
                if (position < 0 || position > this.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(position));
                }

                return UnderlyingText[UnderlyingSpan.Start + position];
            }
        }

        public override string ToString(TextSpan span)
        {
            if (!ValidateSubSpan(span))
                return string.Empty;

            return UnderlyingText.ToString(GetCompositeSpan(span.Start, span.Length));
        }

        public override SourceText GetSubText(TextSpan span)
        {
            if (!ValidateSubSpan(span))
                return From(string.Empty, Encoding, ChecksumAlgorithm);

            return new SubText(UnderlyingText, GetCompositeSpan(span.Start, span.Length));
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (!ValidateCopyToArguments(sourceIndex, destination, destinationIndex, count))
                return;

            var span = GetCompositeSpan(sourceIndex, count);
            UnderlyingText.CopyTo(span.Start, destination, destinationIndex, span.Length);
        }

        public override void Write(TextWriter writer, TextSpan span, CancellationToken cancellationToken = default)
        {
            if (!ValidateWriteArguments(writer, span))
                return;

            UnderlyingText.Write(writer, GetCompositeSpan(span.Start, span.Length), cancellationToken);
        }

        private TextSpan GetCompositeSpan(int start, int length)
        {
            int compositeStart = Math.Min(UnderlyingText.Length, UnderlyingSpan.Start + start);
            int compositeEnd = Math.Min(UnderlyingText.Length, compositeStart + length);
            return new TextSpan(compositeStart, compositeEnd - compositeStart);
        }
    }
}
