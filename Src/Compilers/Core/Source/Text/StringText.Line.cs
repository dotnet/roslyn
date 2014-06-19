//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
using Roslyn.Utilities;

namespace Roslyn.Compilers
{
    public partial class StringText
    {
        /// <summary>
        /// StringText implementation of ITextLine
        /// </summary>
        internal sealed class Line : ITextLine
        {
            private readonly StringText text;
            private readonly TextSpan textSpan;
            private readonly int lineBreakLength;
            private readonly int lineNumber;

            internal Line(StringText text, TextSpan body, int lineBreakLength, int lineNumber)
            {
                Contract.ThrowIfNull(text);
                Contract.ThrowIfFalse(lineBreakLength >= 0);
                Contract.Requires(lineNumber >= 0);
                this.text = text;
                this.textSpan = body;
                this.lineBreakLength = lineBreakLength;
                this.lineNumber = lineNumber;
            }

            /// <summary>
            /// Create a new TextLineSpan from the given parameters
            /// </summary>
            /// <param name="text">StringText this Line is a part of</param>
            /// <param name="start">Start position of the TextLineSpan</param>
            /// <param name="length">Length of the span not including the line break</param>
            /// <param name="lineBreakLength">Length of the line break section of the line</param>
            /// <param name="lineNumber">Line number of this line.</param>
            internal Line(StringText text, int start, int length, int lineBreakLength, int lineNumber)
            {
                Contract.ThrowIfNull(text);
                Contract.ThrowIfFalse(start >= 0);
                Contract.ThrowIfFalse(length >= 0);
                Contract.ThrowIfFalse(lineBreakLength >= 0);
                Contract.Requires(lineNumber >= 0);
                this.text = text;
                this.textSpan = new TextSpan(start, length);
                this.lineBreakLength = lineBreakLength;
                this.lineNumber = lineNumber;
            }

            #region ITextLine

            public int Start
            {
                get { return textSpan.Start; }
            }

            public int End
            {
                get { return textSpan.End; }
            }

            public int EndIncludingLineBreak
            {
                get { return End + lineBreakLength; }
            }

            public TextSpan Extent
            {
                get { return textSpan; }
            }

            public TextSpan ExtentIncludingLineBreak
            {
                get { return TextSpan.FromBounds(Start, EndIncludingLineBreak); }
            }

            public string GetText()
            {
                return text.GetText(textSpan);
            }

            public int LineNumber
            {
                get
                {
                    return lineNumber;
                }
            }

            #endregion
        }
    }
}