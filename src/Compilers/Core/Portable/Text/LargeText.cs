// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// A <see cref="SourceText"/> optimized for very large sources. The text is stored as
    /// a list of chunks (char arrays).
    /// </summary>
    internal sealed class LargeText : SourceText
    {
        /// <remarks>
        /// internal for unit testing
        /// </remarks>
        internal const int ChunkSize = SourceText.LargeObjectHeapLimitInChars; // 40K Unicode chars is 80KB which is less than the large object heap limit.

        private readonly ImmutableArray<char[]> _chunks;
        private readonly int[] _chunkStartOffsets;
        private readonly int _length;
        private readonly Encoding? _encodingOpt;

        internal LargeText(ImmutableArray<char[]> chunks, Encoding? encodingOpt, ImmutableArray<byte> checksum, SourceHashAlgorithm checksumAlgorithm, ImmutableArray<byte> embeddedTextBlob)
            : base(checksum, checksumAlgorithm, embeddedTextBlob)
        {
            _chunks = chunks;
            _encodingOpt = encodingOpt;
            _chunkStartOffsets = new int[chunks.Length];

            int offset = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                _chunkStartOffsets[i] = offset;
                offset += chunks[i].Length;
            }

            _length = offset;
        }

        internal LargeText(ImmutableArray<char[]> chunks, Encoding? encodingOpt, SourceHashAlgorithm checksumAlgorithm)
            : this(chunks, encodingOpt, default(ImmutableArray<byte>), checksumAlgorithm, default(ImmutableArray<byte>))
        {
        }

        internal static SourceText Decode(Stream stream, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, bool throwIfBinaryDetected, bool canBeEmbedded)
        {
            stream.Seek(0, SeekOrigin.Begin);

            long longLength = stream.Length;
            if (longLength == 0)
            {
                return SourceText.From(string.Empty, encoding, checksumAlgorithm);
            }

            var maxCharRemainingGuess = encoding.GetMaxCharCountOrThrowIfHuge(stream);
            Debug.Assert(longLength > 0 && longLength <= int.MaxValue); // GetMaxCharCountOrThrowIfHuge should have thrown.
            int length = (int)longLength;

            using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: Math.Min(length, 4096), leaveOpen: true))
            {
                var chunks = ReadChunksFromTextReader(reader, maxCharRemainingGuess, throwIfBinaryDetected);

                // We must compute the checksum and embedded text blob now while we still have the original bytes in hand.
                // We cannot re-encode to obtain checksum and blob as the encoding is not guaranteed to round-trip.
                var checksum = CalculateChecksum(stream, checksumAlgorithm);
                var embeddedTextBlob = canBeEmbedded ? EmbeddedText.CreateBlob(stream) : default(ImmutableArray<byte>);
                return new LargeText(chunks, reader.CurrentEncoding, checksum, checksumAlgorithm, embeddedTextBlob);
            }
        }

        internal static SourceText Decode(TextReader reader, int length, Encoding? encodingOpt, SourceHashAlgorithm checksumAlgorithm)
        {
            if (length == 0)
            {
                return SourceText.From(string.Empty, encodingOpt, checksumAlgorithm);
            }

            // throwIfBinaryDetected == false since we are given text reader from the beginning
            var chunks = ReadChunksFromTextReader(reader, length, throwIfBinaryDetected: false);

            return new LargeText(chunks, encodingOpt, checksumAlgorithm);
        }

        private static ImmutableArray<char[]> ReadChunksFromTextReader(TextReader reader, int maxCharRemainingGuess, bool throwIfBinaryDetected)
        {
            var chunks = ArrayBuilder<char[]>.GetInstance(1 + maxCharRemainingGuess / ChunkSize);

            while (reader.Peek() != -1)
            {
                var nextChunkSize = ChunkSize;
                if (maxCharRemainingGuess < ChunkSize)
                {
                    // maxCharRemainingGuess typically overestimates a little
                    // so we will first fill a slightly smaller (maxCharRemainingGuess - 64) chunk
                    // and then use 64 char tail, which is likely to be resized.
                    nextChunkSize = Math.Max(maxCharRemainingGuess - 64, 64);
                }

                char[] chunk = new char[nextChunkSize];

                int charsRead = reader.ReadBlock(chunk, 0, chunk.Length);
                if (charsRead == 0)
                {
                    break;
                }

                maxCharRemainingGuess -= charsRead;

                if (charsRead < chunk.Length)
                {
                    Array.Resize(ref chunk, charsRead);
                }

                // Check for binary files
                if (throwIfBinaryDetected && IsBinary(chunk))
                {
                    throw new InvalidDataException();
                }

                chunks.Add(chunk);
            }

            return chunks.ToImmutableAndFree();
        }

        private int GetIndexFromPosition(int position)
        {
            // Binary search to find the chunk that contains the given position.
            int idx = _chunkStartOffsets.BinarySearch(position);
            return idx >= 0 ? idx : (~idx - 1);
        }

        public override char this[int position]
        {
            get
            {
                int i = GetIndexFromPosition(position);
                return _chunks[i][position - _chunkStartOffsets[i]];
            }
        }

        public override Encoding? Encoding => _encodingOpt;

        public override int Length => _length;

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (count == 0)
            {
                return;
            }

            int chunkIndex = GetIndexFromPosition(sourceIndex);
            int chunkStartOffset = sourceIndex - _chunkStartOffsets[chunkIndex];
            while (true)
            {
                var chunk = _chunks[chunkIndex];
                int charsToCopy = Math.Min(chunk.Length - chunkStartOffset, count);
                Array.Copy(chunk, chunkStartOffset, destination, destinationIndex, charsToCopy);
                count -= charsToCopy;
                if (count <= 0)
                {
                    break;
                }

                destinationIndex += charsToCopy;
                chunkStartOffset = 0;
                chunkIndex++;
            }
        }

        public override void Write(TextWriter writer, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (span.Start < 0 || span.Start > _length || span.End > _length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            int count = span.Length;
            if (count == 0)
            {
                return;
            }

            var chunkWriter = writer as LargeTextWriter;

            int chunkIndex = GetIndexFromPosition(span.Start);
            int chunkStartOffset = span.Start - _chunkStartOffsets[chunkIndex];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var chunk = _chunks[chunkIndex];
                int charsToWrite = Math.Min(chunk.Length - chunkStartOffset, count);

                if (chunkWriter != null && chunkStartOffset == 0 && charsToWrite == chunk.Length)
                {
                    // reuse entire chunk
                    chunkWriter.AppendChunk(chunk);
                }
                else
                {
                    writer.Write(chunk, chunkStartOffset, charsToWrite);
                }

                count -= charsToWrite;
                if (count <= 0)
                {
                    break;
                }

                chunkStartOffset = 0;
                chunkIndex++;
            }
        }

        /// <summary>
        /// Called from <see cref="SourceText.Lines"/> to initialize the <see cref="TextLineCollection"/>. Thereafter,
        /// the collection is cached.
        /// </summary>
        /// <returns>A new <see cref="TextLineCollection"/> representing the individual text lines.</returns>
        protected override TextLineCollection GetLinesCore()
        {
            return new LineInfo(this, ParseLineStarts());
        }

        private int[] ParseLineStarts()
        {
            var position = 0;
            var index = 0;
            var lastCr = -1;
            var arrayBuilder = ArrayBuilder<int>.GetInstance();

            // The following loop goes through every character in the text. It is highly
            // performance critical, and thus inlines knowledge about common line breaks
            // and non-line breaks.
            foreach (var chunk in _chunks)
            {
                foreach (var c in chunk)
                {
                    index++;

                    // Common case - ASCII & not a line break
                    const uint bias = '\r' + 1;
                    if (unchecked(c - bias) <= (127 - bias))
                    {
                        continue;
                    }

                    switch (c)
                    {
                        case '\r':
                            lastCr = index;
                            goto line_break;

                        case '\n':
                            // Assumes that the only 2-char line break sequence is CR+LF
                            if (lastCr == (index - 1))
                            {
                                position = index;
                                break;
                            }

                            goto line_break;

                        case '\u0085':
                        case '\u2028':
                        case '\u2029':
line_break:
                            arrayBuilder.Add(position);
                            position = index;
                            break;
                    }
                }
            }

            // Create a start for the final line.  
            arrayBuilder.Add(position);
            return arrayBuilder.ToArrayAndFree();
        }
    }
}
