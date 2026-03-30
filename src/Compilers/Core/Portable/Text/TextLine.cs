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
        private readonly int _start;

        // Top 2 bits encode line break length (0, 1, or 2), bottom 30 bits store the end position
        private readonly int _endAndLineBreakLength;

        // Mask for extracting the end position (bottom 30 bits)
        private const int EndPositionMask = 0x3FFFFFFF;
        // Shift amount for line break length (top 2 bits)
        private const int LineBreakLengthShift = 30;

        private TextLine(SourceText text, int start, int end, int lineBreakLength)
        {
            Debug.Assert(lineBreakLength is >= 0 and <= 2, "Line break length must be 0, 1, or 2");
            Debug.Assert(end >= 0 && end <= EndPositionMask, "End position out of range for bit packing");

            _text = text;
            _start = start;

            // Pack: top 2 bits = line break length, bottom 30 bits = end position
            _endAndLineBreakLength = (lineBreakLength << LineBreakLengthShift) | end;
        }

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

                // Calculate lineEnd without line break and determine line break length
                int lineEnd = span.End;
                int lineBreakLength = 0;
                if (lineEnd > span.Start && TextUtilities.IsAnyLineBreakCharacter(text[lineEnd - 1]))
                {
                    // Span already includes line break at the end - need to exclude it (and maybe the position before it)
                    TextUtilities.GetStartAndLengthOfLineBreakEndingAt(text, span.End - 1, out lineEnd, out lineBreakLength);
                }
                else if (lineEnd < text.Length)
                {
                    // Not at EOF - must have a line break
                    lineBreakLength = TextUtilities.GetLengthOfLineBreak(text, lineEnd);
                    if (lineBreakLength == 0)
                    {
                        // Span doesn't include line break and we're not at EOF - invalid span
                        throw new ArgumentOutOfRangeException(nameof(span), CodeAnalysisResources.SpanDoesNotIncludeEndOfLine);
                    }
                }

                return new TextLine(text, span.Start, lineEnd, lineBreakLength);
            }
            else
            {
                return new TextLine(text, 0, 0, 0);
            }
        }

        // Do not use unless you are certain the span you are passing in is valid!
        // This was added to allow SourceText.LineInfo's indexer to directly create TextLines
        // without the performance implications of calling FromSpan.
        internal static TextLine FromSpanUnsafe(SourceText text, TextSpan span)
        {
            Debug.Assert(span.Start == 0 || TextUtilities.IsAnyLineBreakCharacter(text[span.Start - 1]));
            Debug.Assert(span.End == text.Length || TextUtilities.IsAnyLineBreakCharacter(text[span.End - 1]));

            // Calculate lineEnd without line break and determine line break length
            int lineEnd = span.End;
            int lineBreakLength = 0;
            if (lineEnd > span.Start)
            {
                TextUtilities.GetStartAndLengthOfLineBreakEndingAt(text, lineEnd - 1, out lineEnd, out lineBreakLength);
            }

            return new TextLine(text, span.Start, lineEnd, lineBreakLength);
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
                return _text?.Lines.IndexOf(_start) ?? 0;
            }
        }

        /// <summary>
        /// Gets the start position of the line.
        /// </summary>
        public int Start
        {
            get { return _start; }
        }

        /// <summary>
        /// Gets the end position of the line not including the line break.
        /// </summary>
        public int End
        {
            get { return _endAndLineBreakLength & EndPositionMask; }
        }

        private int LineBreakLength
        {
            get
            {
                // Extract line break length from top 2 bits using unsigned right shift
                return _endAndLineBreakLength >>> LineBreakLengthShift;
            }
        }

        /// <summary>
        /// Gets the end position of the line including the line break.
        /// </summary>
        public int EndIncludingLineBreak
        {
            get { return this.End + this.LineBreakLength; }
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
            // Both _end values include packed line break length, so direct comparison is correct
            return other._text == _text
                && other._start == _start
                && other._endAndLineBreakLength == _endAndLineBreakLength;
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
            return Hash.Combine(_text, Hash.Combine(_start, _endAndLineBreakLength));
        }
    }
}
