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
        //   Bits 63-62  (2 bits): line break length at the end of this line (0 = no break / last line,
        //                         1 = single-char break like \n, \r, etc., 2 = the \r\n Windows pair)
        //   Bits 61-31  (31 bits): start position
        //   Bits 30-0   (31 bits): total length including break = EndIncludingLineBreak - Start
        private readonly ulong _data;

        private const int BreakLenShift = 62;
        private const int StartShift = 31;
        private const ulong RawValueMask = 0x7FFFFFFFUL; // 31 bits, used for both start and length

        private TextLine(SourceText text, ulong data)
        {
            _text = text;
            _data = data;
        }

        private static ulong Pack(int start, int totalLength, int lineBreakLength)
            => ((ulong)(uint)lineBreakLength << BreakLenShift) | ((ulong)(uint)start << StartShift) | (uint)totalLength;

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

                var lineBreakLen = 0;
                if (span.End > span.Start && TextUtilities.IsAnyLineBreakCharacter(text[span.End - 1]))
                {
                    // End already includes line break - determine its length
                    TextUtilities.GetStartAndLengthOfLineBreakEndingAt(text, span.End - 1, out _, out lineBreakLen);
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

                return new TextLine(text, Pack(span.Start, span.Length, lineBreakLen));
            }
            else
            {
                return new TextLine(text, Pack(0, 0, 0));
            }
        }

        // Do not use unless you are certain the span you are passing in is valid!
        // This was added to allow SourceText.LineInfo's indexer to directly create TextLines
        // without the performance implications of calling FromSpan.
        internal static TextLine FromSpanUnsafe(SourceText text, TextSpan span, int lineBreakLength)
        {
            Debug.Assert(span.Start == 0 || TextUtilities.IsAnyLineBreakCharacter(text[span.Start - 1]));
            Debug.Assert(span.End == text.Length || TextUtilities.IsAnyLineBreakCharacter(text[span.End - 1]));
            Debug.Assert(lineBreakLength is >= 0 and <= 2);
            return new TextLine(text, Pack(span.Start, span.Length, lineBreakLength));
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
            get { return (int)((_data >>> StartShift) & RawValueMask); }
        }

        /// <summary>
        /// Gets the length of the line break sequence at the end of this line (0, 1, or 2).
        /// </summary>
        internal int LineBreakLength
        {
            get { return (int)(_data >>> BreakLenShift); }
        }

        /// <summary>
        /// Gets the end position of the line not including the line break.
        /// </summary>
        public int End
        {
            get { return EndIncludingLineBreak - LineBreakLength; }
        }

        /// <summary>
        /// Gets the end position of the line including the line break.
        /// </summary>
        public int EndIncludingLineBreak
        {
            get { return Start + (int)(_data & RawValueMask); }
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
            return other._text == _text && other._data == _data;
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
            return Hash.Combine(_text, Hash.Combine((int)_data, (int)(_data >>> 32)));
        }
    }
}
