// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

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

        protected override TextLineCollection GetLinesCore()
            => new SubTextLineInfo(this);

        public override string ToString(TextSpan span)
        {
            CheckSubSpan(span);

            return UnderlyingText.ToString(GetCompositeSpan(span.Start, span.Length));
        }

        public override SourceText GetSubText(TextSpan span)
        {
            CheckSubSpan(span);

            return new SubText(UnderlyingText, GetCompositeSpan(span.Start, span.Length));
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            var span = GetCompositeSpan(sourceIndex, count);
            UnderlyingText.CopyTo(span.Start, destination, destinationIndex, span.Length);
        }

        private TextSpan GetCompositeSpan(int start, int length)
        {
            int compositeStart = Math.Min(UnderlyingText.Length, UnderlyingSpan.Start + start);
            int compositeEnd = Math.Min(UnderlyingText.Length, compositeStart + length);
            return new TextSpan(compositeStart, compositeEnd - compositeStart);
        }

        /// <summary>
        /// Delegates to the SubText's <see cref="UnderlyingText"/> to determine line information.
        /// </summary>
        private sealed class SubTextLineInfo : TextLineCollection
        {
            private readonly SubText _subText;
            private readonly int _startLineNumberInUnderlyingText;
            private readonly int _lineCount;
            private readonly bool _startsWithinSplitCRLF;
            private readonly bool _endsWithinSplitCRLF;

            public SubTextLineInfo(SubText subText)
            {
                _subText = subText;

                var startLineInUnderlyingText = _subText.UnderlyingText.Lines.GetLineFromPosition(_subText.UnderlyingSpan.Start);
                var endLineInUnderlyingText = _subText.UnderlyingText.Lines.GetLineFromPosition(_subText.UnderlyingSpan.End);

                _startLineNumberInUnderlyingText = startLineInUnderlyingText.LineNumber;
                _lineCount = (endLineInUnderlyingText.LineNumber - _startLineNumberInUnderlyingText) + 1;

                var underlyingSpanStart = _subText.UnderlyingSpan.Start;
                if (underlyingSpanStart == startLineInUnderlyingText.End + 1 &&
                    underlyingSpanStart == startLineInUnderlyingText.EndIncludingLineBreak - 1)
                {
                    Debug.Assert(_subText.UnderlyingText[underlyingSpanStart - 1] == '\r' && _subText.UnderlyingText[underlyingSpanStart] == '\n');
                    _startsWithinSplitCRLF = true;
                }

                var underlyingSpanEnd = _subText.UnderlyingSpan.End;
                if (underlyingSpanEnd == endLineInUnderlyingText.End + 1 &&
                    underlyingSpanEnd == endLineInUnderlyingText.EndIncludingLineBreak - 1)
                {
                    Debug.Assert(_subText.UnderlyingText[underlyingSpanEnd - 1] == '\r' && _subText.UnderlyingText[underlyingSpanEnd] == '\n');
                    _endsWithinSplitCRLF = true;

                    // If this subtext ends in the middle of a CR/LF, then this object should view that CR as a separate line
                    // whereas the UnderlyingText would not.
                    _lineCount += 1;
                }
            }

            public override TextLine this[int lineNumber]
            {
                get
                {
                    if (lineNumber < 0 || lineNumber >= _lineCount)
                    {
                        throw new ArgumentOutOfRangeException(nameof(lineNumber));
                    }

                    if (_endsWithinSplitCRLF && lineNumber == _lineCount - 1)
                    {
                        // Special case splitting the CRLF at the end as the UnderlyingText doesn't view the position
                        // after between the \r and \n as on a new line whereas this subtext doesn't contain the \n
                        // and needs to view that position as on a new line.
                        return TextLine.FromSpanUnsafe(_subText, new TextSpan(_subText.UnderlyingSpan.End, 0));
                    }

                    var underlyingTextLine = _subText.UnderlyingText.Lines[lineNumber + _startLineNumberInUnderlyingText];

                    // Consider input "a\r\nb" where ST1 contains "\a\r" and ST2 contains "\n\b", and requested lineNumber
                    // per this table:
                    // ----------------------------------------------------------------------------------------------------------------
                    // | SubText | lineNumber | underlyingTextLine | _subText          | underlyingTextLine       | _subText          |
                    // |         |            |   .Start           |   .UnderlyingSpan |   .EndIncludingLineBreak |   .UnderlyingSpan |
                    // |         |            |                    |   .Start          |                          |   .End            |
                    // |---------------------------------------------------------------------------------------------------------------
                    // |   ST1   |     0      |         0          |         0         |            3             |         2         |
                    // |   ST2   |     0      |         0          |         2         |            3             |         4         |
                    // |   ST2   |     1      |         3          |         2         |            4             |         4         |
                    // ----------------------------------------------------------------------------------------------------------------

                    // These two variables represent this subtext's view on the start/end of the requested line,
                    // but in the coordinate space of _subText.UnderlyingText.
                    var startInUnderlyingText = Math.Max(underlyingTextLine.Start, _subText.UnderlyingSpan.Start);
                    var endInUnderlyingText = Math.Min(underlyingTextLine.EndIncludingLineBreak, _subText.UnderlyingSpan.End);

                    // This variable represent this subtext's view on start of the requested line,
                    // in it's coordinate space
                    var startInSubText = startInUnderlyingText - _subText.UnderlyingSpan.Start;

                    var length = endInUnderlyingText - startInUnderlyingText;
                    var resultLine = TextLine.FromSpanUnsafe(_subText, new TextSpan(startInSubText, length));

                    var shouldContainLineBreak = (lineNumber != _lineCount - 1);
                    var resultContainsLineBreak = resultLine.EndIncludingLineBreak > resultLine.End;

                    if (shouldContainLineBreak != resultContainsLineBreak)
                    {
                        throw new InvalidOperationException();
                    }

                    // Assert resultLine only has line breaks in the appropriate locations
                    Debug.Assert(resultLine.ToString().All(static c => !TextUtilities.IsAnyLineBreakCharacter(c)));

                    return resultLine;
                }
            }

            public override int Count => _lineCount;

            /// <summary>
            /// Determines the line number of a position in this SubText
            /// </summary>
            public override int IndexOf(int position)
            {
                if (position < 0 || position > _subText.UnderlyingSpan.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(position));
                }

                var underlyingPosition = position + _subText.UnderlyingSpan.Start;
                var underlyingLineNumber = _subText.UnderlyingText.Lines.IndexOf(underlyingPosition);

                if (_startsWithinSplitCRLF && position != 0)
                {
                    // The \n contributes a line to the count in this subtext, but not in the UnderlyingText.
                    underlyingLineNumber += 1;
                }

                return underlyingLineNumber - _startLineNumberInUnderlyingText;
            }
        }
    }
}
