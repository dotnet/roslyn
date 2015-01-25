// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// An SourceText that represents a subrange of another SourceText.
    /// </summary>
    internal sealed class SubText : SourceText
    {
        private readonly SourceText _text;
        private readonly TextSpan _span;

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
                throw new ArgumentException(nameof(span));
            }

            _text = text;
            _span = span;
        }

        public override Encoding Encoding
        {
            get { return _text.Encoding; }
        }

        public SourceText UnderlyingText
        {
            get { return _text; }
        }

        public TextSpan UnderlyingSpan
        {
            get { return _span; }
        }

        public override int Length
        {
            get { return _span.Length; }
        }

        public override char this[int position]
        {
            get
            {
                if (position < 0 || position > this.Length)
                {
                    throw new ArgumentOutOfRangeException("position");
                }

                return _text[_span.Start + position];
            }
        }

        public override string ToString(TextSpan span)
        {
            CheckSubSpan(span);

            return _text.ToString(GetCompositeSpan(span.Start, span.Length));
        }

        public override SourceText GetSubText(TextSpan span)
        {
            CheckSubSpan(span);

            return new SubText(_text, GetCompositeSpan(span.Start, span.Length));
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            var span = GetCompositeSpan(sourceIndex, count);
            _text.CopyTo(span.Start, destination, destinationIndex, span.Length);
        }

        private TextSpan GetCompositeSpan(int start, int length)
        {
            int compositeStart = Math.Min(_text.Length, _span.Start + start);
            int compositeEnd = Math.Min(_text.Length, compositeStart + length);
            return new TextSpan(compositeStart, compositeEnd - compositeStart);
        }
    }
}
