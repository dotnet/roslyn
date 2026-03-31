// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Information about the character boundaries of a single line of text.
    /// </summary>
    public readonly struct TextLine : IEquatable<TextLine>
    {
        private readonly SourceText? _text;

        // Encoding (64 bits total):
        //   Bits 63-61 (3 bits): line break length encoding
        //     0 = unknown (compute from text on demand), used by FromSpanUnsafe
        //     1 = line break length is 0
        //     2 = line break length is 1
        //     3 = line break length is 2
        //   Bits 60-30 (31 bits): start position
        //   Bits 29-0  (30 bits): line length
        //     When break len is known (bits 63-61 != 0): length excludes the line break
        //     When break len is unknown (bits 63-61 == 0): length includes the line break
        private readonly ulong _data;

        private const int BreakLenShift = 61;
        private const int StartShift = 30;
        private const ulong StartMask = 0x7FFFFFFFUL;  // 31 bits
        private const ulong LengthMask = 0x3FFFFFFFUL; // 30 bits

        private TextLine(SourceText text, ulong data)
        {
            _text = text;
            _data = data;
        }

        private static ulong Pack(int start, int length, int lineBreakLength)
            => ((ulong)(lineBreakLength + 1) << BreakLenShift) | ((ulong)start << StartShift) | (ulong)length;

        /// <summary>
        /// Creates a <see cref="TextLine"/> instance.
        /// </summary>
        /// <param name="text">The source text.</param>
        /// <param name="span">The span of the line.</param>
        /// <returns>An instance of <see cref="TextLine"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The span does not represent a text line.</exception>
        public static TextLine FromSpan(SourceText text, TextSpan span)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (span.Start > text.Length || span.Start < 0 || span.End > text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            if (text.Length > 0)
            {
                // check span is start of line
                if (span.Start > 0 && !TextUtilities.IsAnyLineBreakCharacter(text[span.Start - 1]))
                {
                    throw new ArgumentOutOfRangeException(nameof(span), CodeAnalysisResources.SpanDoesNotIncludeStartOfLine);
                }

                int lineBreakLen = 0;
                if (span.End > span.Start && TextUtilities.IsAnyLineBreakCharacter(text[span.End - 1]))
                {
                    // End already includes line break - determine its length
                    int startLineBreak;
                    TextUtilities.GetStartAndLengthOfLineBreakEndingAt(text, span.End - 1, out startLineBreak, out lineBreakLen);
                }
                else if (span.End < text.Length)
                {
                    lineBreakLen = TextUtilities.GetLengthOfLineBreak(text, span.End);
                    if (lineBreakLen > 0)
                    {
                        // adjust span to include line breaks
                        span = new TextSpan(span.Start, span.Length + lineBreakLen);
                    }
                }

                // check end of span is at end of line
                if (span.End < text.Length && lineBreakLen == 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(span), CodeAnalysisResources.SpanDoesNotIncludeEndOfLine);
                }

                return new TextLine(text, Pack(span.Start, span.Length - lineBreakLen, lineBreakLen));
            }
            else
            {
                return new TextLine(text, Pack(0, 0, 0));
            }
        }

        // Do not use unless you are certain the span you are passing in is valid!
        // This was added to allow SourceText.LineInfo's indexer to directly create TextLines
        // without the performance implications of calling FromSpan.
        internal static TextLine FromSpanUnsafe(SourceText text, TextSpan span)
        {
            Debug.Assert(span.Start == 0 || TextUtilities.IsAnyLineBreakCharacter(text[span.Start - 1]));
            Debug.Assert(span.End == text.Length || TextUtilities.IsAnyLineBreakCharacter(text[span.End - 1]));

            // Store total length (including any line break) with unknown encoding;
            // LineBreakLength will be computed from text on demand.
            return new TextLine(text, ((ulong)span.Start << StartShift) | (ulong)(span.End - span.Start));
        }

        /// <summary>
        /// Gets the source text.
        /// </summary>
        public SourceText? Text
        {
            get { return _text; }
        }

        /// <summary>
        /// Gets the zero-based line number.
        /// </summary>
        public int LineNumber
        {
            get
            {
                return _text?.Lines.IndexOf(Start) ?? 0;
            }
        }

        /// <summary>
        /// Gets the start position of the line.
        /// </summary>
        public int Start
        {
            get { return (int)((_data >> StartShift) & StartMask); }
        }

        /// <summary>
        /// Gets the end position of the line not including the line break.
        /// </summary>
        public int End
        {
            get
            {
                int start = Start;
                int rawLength = (int)(_data & LengthMask);
                int encoded = (int)(_data >> BreakLenShift);
                if (encoded != 0)
                    return start + rawLength;
                else
                    return start + rawLength - LineBreakLength;
            }
        }

        private int LineBreakLength
        {
            get
            {
                int encoded = (int)(_data >> BreakLenShift);
                if (encoded != 0)
                    return encoded - 1;

                // Unknown: compute from text
                if (_text == null || _text.Length == 0 || (_data & LengthMask) == 0)
                {
                    return 0;
                }

                int endIncludingBreak = Start + (int)(_data & LengthMask);
                int startLineBreak;
                int lineBreakLength;
                TextUtilities.GetStartAndLengthOfLineBreakEndingAt(_text, endIncludingBreak - 1, out startLineBreak, out lineBreakLength);
                return lineBreakLength;
            }
        }

        /// <summary>
        /// Gets the end position of the line including the line break.
        /// </summary>
        public int EndIncludingLineBreak
        {
            get
            {
                int start = Start;
                int rawLength = (int)(_data & LengthMask);
                int encoded = (int)(_data >> BreakLenShift);
                if (encoded != 0)
                    return start + rawLength + (encoded - 1);
                else
                    return start + rawLength;
            }
        }

        /// <summary>
        /// Gets the line span not including the line break.
        /// </summary>
        public TextSpan Span
        {
            get { return TextSpan.FromBounds(this.Start, this.End); }
        }

        /// <summary>
        /// Gets the line span including the line break.
        /// </summary>
        public TextSpan SpanIncludingLineBreak
        {
            get { return TextSpan.FromBounds(this.Start, this.EndIncludingLineBreak); }
        }

        public override string ToString()
        {
            if (_text == null || _text.Length == 0)
            {
                return string.Empty;
            }
            else
            {
                return _text.ToString(this.Span);
            }
        }

        public static bool operator ==(TextLine left, TextLine right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextLine left, TextLine right)
        {
            return !left.Equals(right);
        }

        public bool Equals(TextLine other)
        {
            if (other._text != _text)
                return false;

            // Fast path: both have known line break length, compare packed data directly
            if ((_data >> BreakLenShift) != 0 && (other._data >> BreakLenShift) != 0)
                return _data == other._data;

            // Slow path: at least one has unknown encoding, compare decoded logical positions
            return Start == other.Start && EndIncludingLineBreak == other.EndIncludingLineBreak;
        }

        public override bool Equals(object? obj)
        {
            if (obj is TextLine)
            {
                return Equals((TextLine)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_text, Hash.Combine(Start, EndIncludingLineBreak));
        }
    }
}
