using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text;
using Roslyn.Utilities;

namespace Roslyn.Compilers
{
    /// <summary>
    /// Implementation of IText based on a <see cref="T:System.IO.Stream"/> of bytes representing unicode characters
    /// </summary>
    public sealed class StreamText : IText, ITextContainer
    {
        /// <summary>
        /// decoded characters from Stream
        /// </summary>
        private string text;

        /// <summary>
        /// We never encode anything, but we need some value to pass to GetEncoding.  All instances can share this object.
        /// </summary>
        private static SimpleEncoderFallback NoEncoderFallback = new SimpleEncoderFallback();
        
        /// <summary>
        /// Collection of spans which represent line text information within the buffer
        /// </summary>
        private ReadOnlyCollection<ITextLine> lazyLines;
        private int[] lazyLineStarts;

        /// <summary>
        /// Initializes an instance of <see cref="T:StreamText"/> with provided bytes.
        /// The following encodings with by automatically detected: BigEndianUnicode, Unicode, UTF8
        /// (with or without byte order mark).  The default windows codepage will be used as a fallback.
        /// </summary>
        public StreamText(Stream data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            DetectEncodingAndDecode(data);
        }

        /// <summary>
        /// Initializes an instance of <see cref="T:StreamText"/> with provided bytes.
        /// </summary>
        public StreamText(Stream data, Encoding encoding)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            Decode(data, encoding);
        }

        /// <summary>
        /// Initializes an instance of <see cref="T:StreamText"/> with the specified file name.
        /// The following encodings with by automatically detected: BigEndianUnicode, Unicode, UTF8
        /// (with or without byte order mark).  The default windows codepage will be used as a fallback.
        /// </summary>
        public StreamText(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            using (var data = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                DetectEncodingAndDecode(data);
            }
        }

        /// <summary>
        /// Initializes an instance of <see cref="T:StreamText"/> with the specified file name.
        /// </summary>
        public StreamText(string path, Encoding encoding)
        {
            if (path == null)
            {
                throw new ArgumentNullException("data");
            }
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            using (var data = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Decode(data, encoding);
            }
        }

        private void DetectEncodingAndDecode(Stream data)
        {
            var encoding = Encoding.Default; // this is the same fallback that the native compilers used (default Windows ANSI codepage)

            // First, look for byte order marks...
            if (data.Length >= 2)
            {
                byte[] byteOrderMark = new byte[2];
                data.Read(byteOrderMark, 0, 2);

                if (0xFE == byteOrderMark[0] && 0xFF == byteOrderMark[1])
                {
                    encoding = Encoding.BigEndianUnicode;
                }
                else if (0xFF == byteOrderMark[0] && 0xFE == byteOrderMark[1])
                {
                    encoding = Encoding.Unicode;
                }
                else if (data.Length >= 3)
                {
                    if (0xEF == byteOrderMark[0] && 0xBB == byteOrderMark[1] && 0xBF == data.ReadByte())
                    {
                        encoding = Encoding.UTF8;
                    }
                }

                // reset the cursor now that we're done looking for byte order marks
                data.Position = 0;
            }

            // If we didn't find a recognized byte order mark, check to see if the file contents are valid UTF-8
            // with no byte order mark.  Detecting UTF-8 with no byte order mark implicitly decodes the entire array
            // to check each byte, so we won't decode again unless we've already detected some other encoding or
            // this is not valid UTF-8
            if (Encoding.Default != encoding  || !TryDecodeUTF8NoBOM(data, out this.text))
            {
                Decode(data, encoding);
            }
        }

        private void Decode(Stream data, Encoding encoding)
        {
            var reader = new StreamReader(data, encoding);
            this.text = reader.ReadToEnd();
        }

        private static bool TryDecodeUTF8NoBOM(Stream data, out string text)
        {
            text = null;

            var decoderFallback = new SimpleDecoderFallback();
            var utf8Encoding = Encoding.GetEncoding(Encoding.UTF8.CodePage,
                NoEncoderFallback, // this is arbitrary, since we'll never encode anything with this instance
                decoderFallback);

            byte[] buffer = new byte[data.Length];
            data.Read(buffer, 0, buffer.Length);
            var decodedCharacters = utf8Encoding.GetString(buffer, 0, buffer.Length);
            
            if (!decoderFallback.WasInvoked)
            {
                text = decodedCharacters;
                return true;
            }

            data.Position = 0;

            return false;
        }

        /// <summary>
        /// We never encode anything, but we need something to pass to GetEncoding
        /// </summary>
        private class SimpleEncoderFallback : EncoderFallback
        {
            public override EncoderFallbackBuffer CreateFallbackBuffer()
            {
                throw new NotImplementedException();
            }

            public override int MaxCharCount
            {
                get { throw new NotImplementedException(); }
            }
        }

        /// <summary>
        /// Simple decoder fallback (just used to detect if there was an error while decoding)
        /// </summary>
        private class SimpleDecoderFallback : DecoderFallback
        {
            public bool WasInvoked { get; private set; }

            public override DecoderFallbackBuffer CreateFallbackBuffer()
            {
                WasInvoked = true;
                return FallbackBuffer;
            }

            public override int MaxCharCount
            {
                get
                {
                    return 0;
                }
            }

            private static SimpleFallbackBuffer FallbackBuffer = new SimpleFallbackBuffer();

            private class SimpleFallbackBuffer : DecoderFallbackBuffer
            {
                public override bool Fallback(byte[] bytesUnknown, int index)
                {
                    return false;
                }

                public override char GetNextChar()
                {
                    throw new NotImplementedException();
                }

                public override bool MovePrevious()
                {
                    return false;
                }

                public override int Remaining
                {
                    get
                    {
                        return 0;
                    }
                }
            }
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
        /// The length of the text represented by <see cref="T:StreamText"/>.
        /// </summary>
        public int Length
        {
            get { return this.text.Length; }
        }

        /// <summary>
        /// The length of the text represented by <see cref="T:StreamText"/>.
        /// </summary>
        public int LineCount
        {
            get { return Lines.Count; }
        }

        /// <summary>
        /// The sequence of lines represented by <see cref="T:StreamText"/>.
        /// </summary>
        IEnumerable<ITextLine> IText.Lines
        {
            get { return this.Lines; }
        }

        private ReadOnlyCollection<ITextLine> Lines
        {
            get 
            { 
                if (lazyLines == null) 
                {
                    Interlocked.CompareExchange(ref lazyLines, new ReadOnlyCollection<ITextLine>(TextUtilities.ParseLineData(this)), null);
                }

                return lazyLines;
            }
        }

        private int[] LineStarts
        {
            get
            {
                if (lazyLineStarts == null)
                {
                    Interlocked.CompareExchange(ref lazyLineStarts, this.Lines.Select(l => l.Start).ToArray(), null);
                }

                return lazyLineStarts;
            }
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
                if (position < 0 || position >= this.text.Length)
                {
                    throw new ArgumentOutOfRangeException("position");
                }

                return this.text[position];
            }
        }

        /// <summary>
        /// Provides a string representation of the StreamText.
        /// </summary>
        public string GetText()
        {
            return this.text;
        }

        /// <summary>
        /// Provides a string representation of the StreamText located within given span.
        /// </summary>
        /// <exception cref="T:ArgumentOutOfRangeException">When given span is outside of the text range.</exception>
        public string GetText(TextSpan span)
        {
            if (span.End > this.text.Length)
            {
                throw new ArgumentOutOfRangeException("span");
            }

            return this.text.Substring(span.Start, span.Length);
        }

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            this.text.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public ITextLine GetLineFromLineNumber(int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= Lines.Count)
            {
                throw new ArgumentOutOfRangeException("lineNumber");
            }

            return Lines[lineNumber];
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

            var lines = this.Lines;
            if (position == this.Length)
            {
                // this can happen when the user tried to get the line of items
                // that are at the absolute end of this text (i.e. the EndOfLine
                // token, or missing tokens that are at the end of the text).
                // In this case, we want the last line in the text.
                return lines[lines.Count-1];
            }

            // Binary search to find the right line
            int lineNumber = LineStarts.BinarySearch(position);
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

        public void Write(TextWriter textWriter)
        {
            textWriter.Write(this.text);
        }

        #endregion

        /// <summary>
        /// Provides a string representation of the StreamText.
        /// </summary>
        public override string ToString()
        {
            return this.text;
        }
    }
}
