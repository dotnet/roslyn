﻿using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// A <see cref="SourceText"/> optimized for very large sources. The text is stored as
    /// a list of chunks (char arrays).
    /// </summary>
    internal sealed class LargeEncodedText : SourceText
    {
        /// <remarks>
        /// internal for unit testing
        /// </remarks>
        internal const int ChunkSize = 40 * 1024; // 40K Unicode chars is 80KB which is less than the large object heap limit.

        private readonly ImmutableArray<char[]> _chunks;
        private readonly int[] _chunkStartOffsets;
        private readonly int _length;
        private readonly Encoding _encoding;

        private LargeEncodedText(ImmutableArray<char[]> chunks, Encoding encoding, ImmutableArray<byte> checksum, SourceHashAlgorithm checksumAlgorithm)
            : base(checksum, checksumAlgorithm)
        {
            _chunks = chunks;
            _encoding = encoding;
            _chunkStartOffsets = new int[chunks.Length];
            int offset = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                _chunkStartOffsets[i] = offset;
                offset += chunks[i].Length;
            }

            _length = offset;
        }

        internal static SourceText Decode(Stream stream, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, bool throwIfBinaryDetected)
        {
            stream.Seek(0, SeekOrigin.Begin);

            int length = (int)stream.Length;
            if (length == 0)
            {
                return SourceText.From(string.Empty, encoding, checksumAlgorithm);
            }

            using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: Math.Min(length, 4096), leaveOpen: true))
            {
                ArrayBuilder<char[]> chunks = ArrayBuilder<char[]>.GetInstance(1 + length / ChunkSize);
                while (!reader.EndOfStream)
                {
                    char[] chunk = new char[ChunkSize];
                    int charsRead = reader.ReadBlock(chunk, 0, ChunkSize);
                    if (charsRead == 0)
                    {
                        break;
                    }

                    if (charsRead < ChunkSize)
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

                return new LargeEncodedText(chunks.ToImmutableAndFree(), reader.CurrentEncoding, default(ImmutableArray<byte>), checksumAlgorithm);
            }
        }

        /// <summary>
        /// Check for occurrence of two consecutive NUL (U+0000) characters.
        /// This is unlikely to appear in genuine text, so it's a good heuristic
        /// to detect binary files.
        /// </summary>
        private static bool IsBinary(char[] chunk)
        {
            // PERF: We can advance two chars at a time unless we find a NUL.
            for (int i = 1; i < chunk.Length;)
            {
                if (chunk[i] == '\0')
                {
                    if (chunk[i - 1] == '\0')
                    {
                        return true;
                    }

                    i += 1;
                }
                else
                {
                    i += 2;
                }
            }

            return false;
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

        public override Encoding Encoding
        {
            get
            {
                return _encoding;
            }
        }

        public override int Length
        {
            get
            {
                return _length;
            }
        }

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

            int chunkIndex = GetIndexFromPosition(span.Start);
            int chunkStartOffset = span.Start - _chunkStartOffsets[chunkIndex];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var chunk = _chunks[chunkIndex];
                int charsToWrite = Math.Min(chunk.Length - chunkStartOffset, count);
                writer.Write(chunk, chunkStartOffset, charsToWrite);
                count -= charsToWrite;
                if (count <= 0)
                {
                    break;
                }

                chunkStartOffset = 0;
                chunkIndex++;
            }
        }
    }
}
