// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Information about the character boundaries of a single line of text.
    /// </summary>
    public struct TextLine : IEquatable<TextLine>
    {
        private readonly SourceText text;
        private readonly int start;
        private readonly int endIncludingBreaks;

        private TextLine(SourceText text, int start, int endIncludingBreaks)
        {
            this.text = text;
            this.start = start;
            this.endIncludingBreaks = endIncludingBreaks;
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
                throw new ArgumentNullException("text");
            }

            if (span.Start > text.Length || span.Start < 0 || span.End > text.Length)
            {
                throw new ArgumentOutOfRangeException("span");
            }

            if (text.Length > 0)
            {
                // check span is start of line
                if (span.Start > 0 && !TextUtilities.IsAnyLineBreakCharacter(text[span.Start - 1]))
                {
                    throw new ArgumentOutOfRangeException(CodeAnalysisResources.SpanDoesNotIncludeStartOfLine);
                }

                bool endIncludesLineBreak = false;
                if (span.End > span.Start)
                {
                    endIncludesLineBreak = TextUtilities.IsAnyLineBreakCharacter(text[span.End - 1]);
                }

                if (!endIncludesLineBreak && span.End < text.Length)
                {
                    var lineBreakLen = TextUtilities.GetLengthOfLineBreak(text, span.End);
                    if (lineBreakLen > 0)
                    {
                        // adjust span to include line breaks
                        endIncludesLineBreak = true;
                        span = new TextSpan(span.Start, span.Length + lineBreakLen);
                    }
                }

                // check end of span is at end of line
                if (span.End < text.Length && !endIncludesLineBreak)
                {
                    throw new ArgumentOutOfRangeException(CodeAnalysisResources.SpanDoesNotIncludeEndOfLine);
                }

                return new TextLine(text, span.Start, span.End);
            }
            else
            {
                return new TextLine(text, 0, 0);
            }
        }

        /// <summary>
        /// Gets the source text.
        /// </summary>
        public SourceText Text
        {
            get { return this.text; }
        }

        /// <summary>
        /// Gets the zero-based line number.
        /// </summary>
        public int LineNumber
        {
            get
            {
                if (this.text != null)
                {
                    return this.text.Lines.IndexOf(this.start);
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the start position of the line.
        /// </summary>
        public int Start
        {
            get { return this.start; }
        }

        /// <summary>
        /// Gets the end position of the line not including the line break.
        /// </summary>
        public int End
        {
            get { return this.endIncludingBreaks - this.LineBreakLength; }
        }

        private int LineBreakLength
        {
            get
            {
                if (this.text == null || this.text.Length == 0 || this.endIncludingBreaks == this.start)
                {
                    return 0;
                }
                else
                {
                    int startLineBreak;
                    int lineBreakLength;
                    TextUtilities.GetStartAndLengthOfLineBreakEndingAt(this.text, this.endIncludingBreaks - 1, out startLineBreak, out lineBreakLength);
                    return lineBreakLength;
                }
            }
        }

        /// <summary>
        /// Gets the end position of the line including the line break.
        /// </summary>
        public int EndIncludingLineBreak
        {
            get { return this.endIncludingBreaks; }
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
            if (this.text == null || this.text.Length == 0)
            {
                return string.Empty;
            }
            else
            {
                return this.text.ToString(this.Span);
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
            return other.text == this.text
                && other.start == this.start
                && other.endIncludingBreaks == this.endIncludingBreaks;
        }

        public override bool Equals(object obj)
        {
            if (obj is TextLine)
            {
                return Equals((TextLine)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.text, Hash.Combine(this.start, this.endIncludingBreaks));
        }
    }
}