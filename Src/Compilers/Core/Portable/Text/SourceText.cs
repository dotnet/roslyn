// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
        private const int CharBufferSize = 32 * 1024;
        private const int CharBufferCount = 5;

        private static readonly ObjectPool<char[]> CharArrayPool = new ObjectPool<char[]>(() => new char[CharBufferSize], CharBufferCount);

        private SourceTextContainer lazyContainer;
        private LineInfo lazyLineInfo;
        private ImmutableArray<byte> lazySha1Checksum;

        protected SourceText(ImmutableArray<byte> sha1Checksum = default(ImmutableArray<byte>), SourceTextContainer container = null)
        {
            if (!sha1Checksum.IsDefault && sha1Checksum.Length != CryptographicHashProvider.Sha1HashSize)
            {
                throw new ArgumentException(CodeAnalysisResources.InvalidSHA1Hash, "sha1Checksum");
            }

            this.lazySha1Checksum = sha1Checksum;
            this.lazyContainer = container;
        }

        /// <summary>
        /// Constructs a <see cref="SourceText"/> from text in a string.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="encoding">
        /// Encoding of the file that the <paramref name="text"/> was read from or is going to be saved to.
        /// <c>null</c> if the encoding is unspecified.
        /// If the encoding is not specified the resulting <see cref="SourceText"/> isn't debuggable.
        /// If an encoding-less <see cref="SourceText"/> is written to a file a <see cref="Encoding.UTF8"/> shall be used as a default.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public static SourceText From(string text, Encoding encoding = null)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            return new StringText(text, encoding);
        }

        /// <summary>
        /// Constructs a <see cref="SourceText"/> from stream content.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="encoding">
        /// Data encoding to use if the stream doesn't start with Byte Order Mark specifying the encoding.
        /// <see cref="Encoding.UTF8"/> if not specified.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> doesn't support reading or seeking.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <remarks>Reads from the beginning of the stream. Leaves the stream open.</remarks>
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

            encoding = encoding ?? Encoding.UTF8;

            // TODO: unify encoding detection with EncodedStringText

            stream.Seek(0, SeekOrigin.Begin);
            string text;
            using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            {
                text = reader.ReadToEnd();
            }

            return new StringText(text, encoding, CryptographicHashProvider.ComputeSha1(stream));
        }

        /// <summary>
        /// Encoding of the file that the text was read from or is going to be saved to.
        /// <c>null</c> if the encoding is unspecified.
        /// </summary>
        /// <remarks>
        /// If the encoding is not specified the source isn't debuggable.
        /// If an encoding-less <see cref="SourceText"/> is written to a file a <see cref="Encoding.UTF8"/> shall be used as a default.
        /// </remarks>
        public abstract Encoding Encoding { get; }

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
                if (this.lazyContainer == null)
                {
                    Interlocked.CompareExchange(ref this.lazyContainer, new StaticContainer(this), null);
                }

                return this.lazyContainer;
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
                return SourceText.From(string.Empty, this.Encoding);
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
        public void Write(TextWriter textWriter, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.Write(textWriter, new TextSpan(0, this.Length), cancellationToken);
        }

        /// <summary>
        /// Write a span of text to a text writer.
        /// </summary>
        public virtual void Write(TextWriter writer, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSubSpan(span);

            var buffer = CharArrayPool.Allocate();
            try
            {
                int offset = Math.Min(this.Length, span.Start);
                int length = Math.Min(this.Length, span.End) - offset;
                while (offset < length)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int count = Math.Min(buffer.Length, length - offset);
                    this.CopyTo(offset, buffer, 0, count);
                    writer.Write(buffer, 0, count);
                    offset += count;
                }
            }
            finally
            {
                CharArrayPool.Free(buffer);
            }
        }

        internal ImmutableArray<byte> GetSha1Checksum()
        {
            if (this.lazySha1Checksum.IsDefault)
            {
                // we shouldn't be asking for a checksum of encoding-less source text:
                Debug.Assert(this.Encoding != null);

                var stream = new MemoryStream();
                using (var writer = new StreamWriter(stream, this.Encoding))
                {
                    this.Write(writer);
                    writer.Flush();
                    stream.Seek(0, SeekOrigin.Begin);
                    ImmutableInterlocked.InterlockedCompareExchange(ref lazySha1Checksum, CryptographicHashProvider.ComputeSha1(stream), default(ImmutableArray<byte>));
                }
            }

            return this.lazySha1Checksum;
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

            var segments = ArrayBuilder<SourceText>.GetInstance();
            var changeRanges = ArrayBuilder<TextChangeRange>.GetInstance();
            int position = 0;

            foreach (var change in changes)
            {
                // there can be no overlapping changes
                if (change.Span.Start < position)
                {
                    throw new ArgumentException(CodeAnalysisResources.ChangesMustBeOrderedAndNotOverlapping, "changes");
                }

                // if we've skipped a range, add
                if (change.Span.Start > position)
                {
                    var subText = this.GetSubText(new TextSpan(position, change.Span.Start - position));
                    CompositeText.AddSegments(segments, subText);
                }

                if (!string.IsNullOrEmpty(change.NewText))
                {
                    var segment = SourceText.From(change.NewText, this.Encoding);
                    CompositeText.AddSegments(segments, segment);
                }

                position = change.Span.End;

                changeRanges.Add(new TextChangeRange(change.Span, change.NewText != null ? change.NewText.Length : 0));
            }

            if (position < this.Length)
            {
                var subText = this.GetSubText(new TextSpan(position, this.Length - position));
                CompositeText.AddSegments(segments, subText);
            }

            return new ChangedText(this, changeRanges.ToImmutableAndFree(), segments.ToImmutableAndFree());
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
                return ImmutableArray.Create(new TextChangeRange(new TextSpan(0, oldText.Length), this.Length));
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

            return textChanges.ToImmutableArrayOrEmpty();
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
                if (this.lazyLineInfo == null)
                {
                    var info = new LineInfo(this, this.ParseLineStarts());
                    Interlocked.CompareExchange(ref this.lazyLineInfo, info, null);
                }

                return this.lazyLineInfo;
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

        /// <summary>
        /// Compares the content with content of another <see cref="SourceText"/>.
        /// </summary>
        public bool ContentEquals(SourceText other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // Checksum may be provided by a subclass, which is thus responsible for passing us a true SHA1 hash.
            ImmutableArray<byte> leftChecksum = this.lazySha1Checksum;
            ImmutableArray<byte> rightChecksum = other.lazySha1Checksum;
            if (!leftChecksum.IsDefault && !rightChecksum.IsDefault && this.Encoding == other.Encoding)
            {
                return leftChecksum.SequenceEqual(rightChecksum);
            }

            return ContentEqualsImpl(other);
        }

        /// <summary>
        /// Implements equality comparison of the content of two different instances of <see cref="SourceText"/>.
        /// </summary>
        protected virtual bool ContentEqualsImpl(SourceText other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (this.Length != other.Length)
            {
                return false;
            }

            var buffer1 = CharArrayPool.Allocate();
            var buffer2 = CharArrayPool.Allocate();
            try
            {
                int position = 0;
                while (position < this.Length)
                {
                    int n = Math.Min(this.Length - position, buffer1.Length);
                    this.CopyTo(position, buffer1, 0, n);
                    other.CopyTo(position, buffer2, 0, n);

                    for (int i = 0; i < n; i++)
                    {
                        if (buffer1[i] != buffer2[i])
                        {
                            return false;
                        }
                    }

                    position += n;
                }

                return true;
            }
            finally
            {
                CharArrayPool.Free(buffer2);
                CharArrayPool.Free(buffer1);
            }
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