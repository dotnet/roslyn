// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// An abstraction of source text.
    /// </summary>
    public abstract class SourceText
    {
        private SourceTextContainer container;
        private LineInfo lineInfo;

        protected SourceText(SourceTextContainer container = null)
        {
            this.container = container;
        }

        /// <summary>
        /// Constructs a <see cref="SourceText"/> from text in a string.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="checksum">
        /// SHA1 checksum of the binary representation of the text. 
        /// Used by the compiler to produce debug information for the corresponding document.
        /// The document won't be debuggable if checksum is not specified.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public static SourceText From(string text, ImmutableArray<byte> checksum = default(ImmutableArray<byte>))
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            if (!checksum.IsDefault && checksum.Length != Hash.Sha1HashSize)
            {
                throw new ArgumentException(CodeAnalysisResources.InvalidSHA1Hash, "checksum");
            }

            return new StringText(text, checksum.IsDefault ? ImmutableArray.Create<byte>() : checksum);
        }

        /// <summary>
        /// Constructs a <see cref="SourceText"/> from stream content.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="encoding">
        /// Data encoding to use unless the stream starts with Byte Order Mark specifying the encoding.
        /// UTF8 if not specified.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> doesn't support reading or seeking.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <remarks>
        /// Reads from the beginning of the stream. Leaves the stream open.
        /// Attaches SHA1 checksum of the binary content to the <see cref="SourceText"/>. 
        /// This information is used by the compiler to produce debug information for the document.
        /// </remarks>
        public static SourceText From(Stream stream, Encoding encoding = null)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (!stream.CanRead || !stream.CanSeek)
            {
                throw new ArgumentException(CodeAnalysisResources.StreamMustSupportReadAndSeek, "stream");
            }

            // TODO: optimize for FileStream (bug 895371)

            stream.Seek(0, SeekOrigin.Begin);
            string text;
            using (var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            {
                text = reader.ReadToEnd();
            }

            return new StringText(text, Hash.ComputeSha1(stream));
        }

        /// <summary>
        /// The length of the text in characters.
        /// </summary>
        public abstract int Length { get; }

        /// <summary>
        /// Returns a character at given position.
        /// </summary>
        /// <param name="position">The position to get the character from.</param>
        /// <returns>The character.</returns>
        /// <exception cref="T:ArgumentOutOfRangeException">When position is negative or 
        /// greater than <see cref="T:"/> length.</exception>
        public abstract char this[int position] { get; }

        /// <summary>
        /// Copy a range of characters from this SourceText to a destination array.
        /// </summary>
        public abstract void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

        /// <summary>
        /// The container of this <see cref="SourceText"/>.
        /// </summary>
        public virtual SourceTextContainer Container
        {
            get
            {
                if (this.container == null)
                {
                    Interlocked.CompareExchange(ref this.container, new StaticContainer(this), null);
                }

                return this.container;
            }
        }

        internal void CheckSubSpan(TextSpan span)
        {
            if (span.Start < 0 || span.Start > this.Length || span.End > this.Length)
            {
                throw new ArgumentOutOfRangeException("span");
            }
        }

        /// <summary>
        /// Gets a <see cref="SourceText"/> that contains the characters in the specified span of this text.
        /// </summary>
        public virtual SourceText GetSubText(TextSpan span)
        {
            CheckSubSpan(span);

            int spanLength = span.Length;
            if (spanLength == 0)
            {
                return SourceText.From(string.Empty);
            }
            else if (spanLength == this.Length && span.Start == 0)
            {
                return this;
            }
            else
            {
                return new SubText(this, span);
            }
        }

        /// <summary>
        /// Returns a <see cref="SourceText"/> that has the contents of this text including and after the start position.
        /// </summary>
        public SourceText GetSubText(int start)
        {
            if (start < 0 || start > this.Length)
            {
                throw new ArgumentOutOfRangeException("start");
            }

            if (start == 0)
            {
                return this;
            }
            else
            {
                return this.GetSubText(new TextSpan(start, this.Length - start));
            }
        }

        /// <summary>
        /// Write this <see cref="SourceText"/> to a text writer.
        /// </summary>
        public void Write(TextWriter textWriter)
        {
            this.Write(textWriter, new TextSpan(0, this.Length));
        }

        /// <summary>
        /// Write a span of text to a text writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="span"></param>
        public virtual void Write(TextWriter writer, TextSpan span)
        {
            CheckSubSpan(span);

            // TODO: get this char[] from a pool?
            var buffer = new char[1024];
            int offset = Math.Min(this.Length, span.Start);
            int length = Math.Min(this.Length, span.End) - offset;
            while (offset < length)
            {
                int count = Math.Min(buffer.Length, length - offset);
                this.CopyTo(offset, buffer, 0, count);
                writer.Write(buffer, 0, count);
                offset += count;
            }
        }

        internal ImmutableArray<byte> GetSha1Checksum()
        {
            return GetSha1ChecksumImpl();
        }

        /// <summary>
        /// Computes SHA1 hash of the text binary representation.
        /// </summary>
        /// <remarks>
        /// Returns <see cref="ImmutableArray{T}.Empty"/> if the binary representation is not available.
        /// </remarks>
        protected virtual ImmutableArray<byte> GetSha1ChecksumImpl()
        {
            return ImmutableArray<byte>.Empty;
        }

        /// <summary>
        /// Provides a string representation of the SourceText.
        /// </summary>
        public override string ToString()
        {
            return ToString(new TextSpan(0, this.Length));
        }

        /// <summary>
        /// Gets a string containing the characters in specified span.
        /// </summary>
        /// <exception cref="T:ArgumentOutOfRangeException">When given span is outside of the text range.</exception>
        public virtual string ToString(TextSpan span)
        {
            CheckSubSpan(span);

            // default implementation constructs text using CopyTo
            var builder = new StringBuilder();
            var buffer = new char[Math.Min(span.Length, 1024)];

            int position = Math.Max(Math.Min(span.Start, this.Length), 0);
            int length = Math.Min(span.End, this.Length) - position;

            while (position < this.Length && length > 0)
            {
                int copyLength = Math.Min(buffer.Length, length);
                this.CopyTo(position, buffer, 0, copyLength);
                builder.Append(buffer, 0, copyLength);
                length -= copyLength;
                position += copyLength;
            }

            return builder.ToString();
        }

        #region Changes

        /// <summary>
        /// Constructs a new SourceText from this text with the specified changes.
        /// </summary>
        public virtual SourceText WithChanges(IEnumerable<TextChange> changes)
        {
            if (changes == null)
            {
                throw new ArgumentNullException("changes");
            }

            if (!changes.Any())
            {
                return this;
            }

            return new ChangedText(this, changes);
        }

        /// <summary>
        /// Constructs a new SourceText from this text with the specified changes.
        /// </summary>
        public SourceText WithChanges(params TextChange[] changes)
        {
            return this.WithChanges((IEnumerable<TextChange>)changes);
        }

        /// <summary>
        /// Returns a new SourceText with the specified span of characters replaced by the new text.
        /// </summary>
        public SourceText Replace(TextSpan span, string newText)
        {
            return this.WithChanges(new TextChange(span, newText));
        }

        /// <summary>
        /// Returns a new SourceText with the specified range of characters replaced by the new text.
        /// </summary>
        public SourceText Replace(int start, int length, string newText)
        {
            return this.Replace(new TextSpan(start, length), newText);
        }

        /// <summary>
        /// Gets the set of <see cref="TextChangeRange"/> that describe how the text changed
        /// between this text an older version. This may be multiple detailed changes
        /// or a single change encompassing the entire text.
        /// </summary>
        public virtual IReadOnlyList<TextChangeRange> GetChangeRanges(SourceText oldText)
        {
            if (oldText == null)
            {
                throw new ArgumentNullException("oldText");
            }

            if (oldText == this)
            {
                return TextChangeRange.NoChanges;
            }
            else
            {
                return ImmutableList.Create(new TextChangeRange(new TextSpan(0, oldText.Length), this.Length));
            }
        }

        /// <summary>
        /// Gets the set of <see cref="T:TextChange"/> that describe how the text changed
        /// between this text and an older version. This may be multiple detailed changes 
        /// or a single change encompassing the entire text.
        /// </summary>
        public virtual IReadOnlyList<TextChange> GetTextChanges(SourceText oldText)
        {
            int newPosDelta = 0;

            var ranges = this.GetChangeRanges(oldText).ToList();
            var textChanges = new List<TextChange>(ranges.Count);

            foreach (var range in ranges)
            {
                var newPos = range.Span.Start + newPosDelta;

                // determine where in the newText this text exists
                string newt;
                if (range.NewLength > 0)
                {
                    var span = new TextSpan(newPos, range.NewLength);
                    newt = this.ToString(span);
                }
                else
                {
                    newt = string.Empty;
                }

                textChanges.Add(new TextChange(range.Span, newt));

                newPosDelta += range.NewLength - range.Span.Length;
            }

            return textChanges.ToImmutableListOrEmpty();
        }

        #endregion

        #region Lines

        /// <summary>
        /// The collection of individual text lines.
        /// </summary>
        public virtual TextLineCollection Lines
        {
            get
            {
                if (this.lineInfo == null)
                {
                    var info = new LineInfo(this, this.ParseLineStarts());
                    System.Threading.Interlocked.CompareExchange(ref this.lineInfo, info, null);
                }

                return this.lineInfo;
            }
        }

        private class LineInfo : TextLineCollection
        {
            private readonly SourceText text;
            private readonly int[] lineStarts;
            private int lastLineNumber = 0;

            public LineInfo(SourceText text, int[] lineStarts)
            {
                this.text = text;
                this.lineStarts = lineStarts;
            }

            public override int Count
            {
                get { return this.lineStarts.Length; }
            }

            public override TextLine this[int index]
            {
                get
                {
                    if (index < 0 || index >= this.lineStarts.Length)
                    {
                        throw new ArgumentOutOfRangeException("index");
                    }

                    int start = lineStarts[index];
                    if (index == lineStarts.Length - 1)
                    {
                        return TextLine.FromSpan(this.text, TextSpan.FromBounds(start, this.text.Length));
                    }
                    else
                    {
                        int end = lineStarts[index + 1];
                        return TextLine.FromSpan(this.text, TextSpan.FromBounds(start, end));
                    }
                }
            }

            public override int IndexOf(int position)
            {
                if (position < 0 || position > this.text.Length)
                {
                    throw new ArgumentOutOfRangeException("position");
                }

                int lineNumber;

                // it is common to ask about position on the same line 
                // as before or on the next couple lines
                var lastLineNumber = this.lastLineNumber;
                if (position >= this.lineStarts[lastLineNumber])
                {
                    var limit = Math.Min(this.lineStarts.Length, lastLineNumber + 4);
                    for (int i = lastLineNumber; i < limit; i++)
                    {
                        if (position < this.lineStarts[i])
                        {
                            lineNumber = i - 1;
                            this.lastLineNumber = lineNumber;
                            return lineNumber;
                        }
                    }
                }

                // Binary search to find the right line
                // if no lines start exactly at position, round to the left
                // EoF position will map to the last line.
                lineNumber = this.lineStarts.BinarySearch(position);
                if (lineNumber < 0)
                {
                    lineNumber = (~lineNumber) - 1;
                }

                this.lastLineNumber = lineNumber;
                return lineNumber;
            }

            public override TextLine GetLineFromPosition(int position)
            {
                return this[IndexOf(position)];
            }
        }

        private int[] ParseLineStarts()
        {
            int length = this.Length;

            // Corner case check
            if (0 == this.Length)
            {
                return new int[] { 0 };
            }

            var position = 0;
            var index = 0;
            var arrayBuilder = ArrayBuilder<int>.GetInstance();
            var lineNumber = 0;

            // The following loop goes through every character in the text. It is highly
            // performance critical, and thus inlines knowledge about common line breaks
            // and non-line breaks.
            while (index < length)
            {
                char c = this[index];
                int lineBreakLength;

                // common case - ASCII & not a line break
                if (c > '\r' & c <= 127)
                {
                    index++;
                    continue;
                }
                else if (c == '\r' && index + 1 < length && this[index + 1] == '\n')
                {
                    lineBreakLength = 2;
                }
                else if (c == '\n')
                {
                    lineBreakLength = 1;
                }
                else
                {
                    lineBreakLength = TextUtilities.GetLengthOfLineBreak(this, index);
                }

                if (0 == lineBreakLength)
                {
                    index++;
                }
                else
                {
                    arrayBuilder.Add(position);
                    index += lineBreakLength;
                    position = index;
                    lineNumber++;
                }
            }

            // Create a start for the final line.  
            arrayBuilder.Add(position);

            return arrayBuilder.ToArrayAndFree();
        }

        #endregion

        private class StaticContainer : SourceTextContainer
        {
            private readonly SourceText text;

            public StaticContainer(SourceText text)
            {
                this.text = text;
            }

            public override SourceText CurrentText
            {
                get { return this.text; }
            }

            public override event EventHandler<TextChangeEventArgs> TextChanged
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
        }
    }
}