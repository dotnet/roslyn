//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Roslyn.Compilers
{
    /// <summary>
    /// Implementation of IText based on a <see cref="T:System.String"/> input
    /// </summary>
    public sealed partial class LazyStringText : IText, ITextContainer
    {
        /// <summary>
        /// Underlying string on which this IText instance is based
        /// </summary>
        private readonly Lazy<string> source;

        /// <summary>
        /// Collection of spans which represent line text information within the buffer
        /// </summary>
        private readonly Lazy<ReadOnlyCollection<ITextLine>> lines;
        private readonly Lazy<int[]> lineStarts;

        /// <summary>
        /// Underlying string which is the source of this IText instance
        /// </summary>
        public string Source
        {
            get
            {
                return source.Value;
            }
        }

        /// <summary>
        /// Initializes an instance of <see cref="T:StringText"/> with provided data.
        /// </summary>
        public LazyStringText(Lazy<string> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("factory");
            }

            source = data;

            // ParseLineData is expensive.
            // we want to delay running it and do not want to run it optimistically.
            // therefore we use ExecutionAndPublication flag
            lines = new Lazy<ReadOnlyCollection<ITextLine>>(
                () => new ReadOnlyCollection<ITextLine>(TextUtilities.ParseLineData(this)),
                LazyThreadSafetyMode.ExecutionAndPublication);

            lineStarts = new Lazy<int[]>(
                () => lines.Value.Select((l) => l.Start).ToArray(),
                LazyThreadSafetyMode.ExecutionAndPublication);

        }

        #region ITextContainer

        IText ITextContainer.CurrentText
        {
            get { return this; }
        }

        event EventHandler<TextChangeEventArgs> ITextContainer.TextChanged
        {
            add
            {
                // do nothing
            }

            remove
            {
                // do nothing
            }
        }

        TextChangeRange[] ITextContainer.GetChanges(IText oldText, IText newText)
        {
            if (oldText == null)
            {
                throw new ArgumentNullException("oldText");
            }

            if (newText == null)
            {
                throw new ArgumentNullException("newText");
            }

            if (oldText == newText)
            {
                return TextChangeRange.NoChanges;
            }
            else
            {
                return new[] { new TextChangeRange(new TextSpan(0, oldText.Length), newText.Length) };
            }
        }

        #endregion

        #region IText

        public ITextContainer Container
        {
            get { return this; }
        }

        /// <summary>
        /// The length of the text represented by <see cref="T:StringText"/>.
        /// </summary>
        public int Length
        {
            get { return this.Source.Length; }
        }

        /// <summary>
        /// The length of the text represented by <see cref="T:StringText"/>.
        /// </summary>
        public int LineCount
        {
            get { return lines.Value.Count; }
        }

        /// <summary>
        /// The sequence of lines represented by <see cref="T:StringText"/>.
        /// </summary>
        public IEnumerable<ITextLine> Lines
        {
            get { return lines.Value; }
        }

        /// <summary>
        /// Returns a character at given position.
        /// </summary>
        /// <param name="position">The position to get the character from.</param>
        /// <returns>The character.</returns>
        /// <exception cref="T:ArgumentOutOfRangeException">When position is negative or 
        /// greater than <see cref="T:"/> length.</exception>
        public char this[int position]
        {
            get
            {
                if (position < 0 || position >= this.Source.Length)
                {
                    throw new ArgumentOutOfRangeException("position");
                }

                return this.Source[position];
            }
        }

        /// <summary>
        /// Provides a string representation of the StringText.
        /// </summary>
        public string GetText()
        {
            return this.Source;
        }

        /// <summary>
        /// Provides a string representation of the StringText located within given span.
        /// </summary>
        /// <exception cref="T:ArgumentOutOfRangeException">When given span is outside of the text range.</exception>
        public string GetText(TextSpan span)
        {
            if (span.End > this.Source.Length)
            {
                throw new ArgumentOutOfRangeException("span");
            }

            return this.Source.Substring(span.Start, span.Length);
        }

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            this.Source.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public ITextLine GetLineFromLineNumber(int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= lines.Value.Count)
            {
                throw new ArgumentOutOfRangeException("lineNumber");
            }

            return lines.Value[lineNumber];
        }

        private ITextLine lastLineFoundForPosition;
        public ITextLine GetLineFromPosition(int position)
        {
            if (position < 0 || position > this.Length)
            {
                throw new ArgumentOutOfRangeException("position");
            }

            // After asking about a location on a particular line
            // it is common to ask about other position in the same line again.
            // try to see if this is the case.
            var lastFound = this.lastLineFoundForPosition;
            if (lastFound != null &&
                lastFound.Start <= position &&
                lastFound.EndIncludingLineBreak > position)
            {
                return lastFound;
            }

            var lines = this.lines.Value;
            if (position == this.Length)
            {
                // this can happen when the user tried to get the line of items
                // that are at the absolute end of this text (i.e. the EndOfLine
                // token, or missing tokens that are at the end of the text).
                // In this case, we want the last line in the text.
                return lines[lines.Count-1];
            }

            // Binary search to find the right line
            int lineNumber = lineStarts.Value.BinarySearch(position);
            if (lineNumber < 0)
            {
                lineNumber = (~lineNumber) - 1;
            }

            var result = lines[lineNumber];
            this.lastLineFoundForPosition = result;
            return result;
        }

        public int GetLineNumberFromPosition(int position)
        {
            return this.GetLineFromPosition(position).LineNumber;
        }

        #endregion

        public void Write(TextWriter textWriter)
        {
            textWriter.Write(this.Source);
        }

        /// <summary>
        /// Provides a string representation of the StringText.
        /// </summary>
        public override string ToString()
        {
            return this.Source;
        }
    }
}
