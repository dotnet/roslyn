// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// An SourceText that represents a subrange of another SourceText.
    /// </summary>
    internal class SubText : SourceText
    {
        private readonly SourceText text;
        private readonly TextSpan span;

        public SubText(SourceText text, TextSpan span)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            if (span.Start < 0
                || span.Start >= text.Length
                || span.End < 0
                || span.End > text.Length)
            {
                throw new ArgumentException("span");
            }

            this.text = text;
            this.span = span;
        }

        public SourceText UnderlyingText
        {
            get { return this.text; }
        }

        public TextSpan UnderlyingSpan
        {
            get { return this.span; }
        }

        public override int Length
        {
            get { return this.span.Length; }
        }

        public override char this[int position]
        {
            get
            {
                if (position < 0 || position > this.Length)
                {
                    throw new ArgumentOutOfRangeException("position");
                }

                return this.text[this.span.Start + position];
            }
        }

        public override string ToString(TextSpan span)
        {
            CheckSubSpan(span);

            return this.text.ToString(GetCompositeSpan(span.Start, span.Length));
        }

        public override SourceText GetSubText(TextSpan span)
        {
            CheckSubSpan(span);

            return new SubText(this.text, GetCompositeSpan(span.Start, span.Length));
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            var span = GetCompositeSpan(sourceIndex, count);
            this.text.CopyTo(span.Start, destination, destinationIndex, span.Length);
        }

        private TextSpan GetCompositeSpan(int start, int length)
        {
            int compositeStart = Math.Min(this.text.Length, this.span.Start + start);
            int compositeEnd = Math.Min(this.text.Length, compositeStart + length);
            return new TextSpan(compositeStart, compositeEnd - compositeStart);
        }
    }
}