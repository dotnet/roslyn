// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class SourceTextExtensions
{
    // char pooled memory : 8K * 256 = 2MB
    private const int CharArrayLength = 4 * 1024;

    /// <summary>
    /// Note: there is a strong invariant that you only get arrays back from this that are exactly <see
    /// cref="CharArrayLength"/> long.  Putting arrays back into this of the wrong length will result in broken
    /// behavior.  Do not expose this pool outside of this class.
    /// </summary>
    private static readonly ObjectPool<char[]> s_charArrayPool = new(() => new char[CharArrayLength], 256);

    public static void GetLineAndOffset(this SourceText text, int position, out int lineNumber, out int offset)
    {
        var line = text.Lines.GetLineFromPosition(position);

        lineNumber = line.LineNumber;
        offset = position - line.Start;
    }

    public static int GetOffset(this SourceText text, int position)
    {
        GetLineAndOffset(text, position, out _, out var offset);
        return offset;
    }

    public static void GetLinesAndOffsets(
        this SourceText text,
        TextSpan textSpan,
        out int startLineNumber,
        out int startOffset,
        out int endLineNumber,
        out int endOffset)
    {
        text.GetLineAndOffset(textSpan.Start, out startLineNumber, out startOffset);
        text.GetLineAndOffset(textSpan.End, out endLineNumber, out endOffset);
    }

    public static TextChangeRange GetEncompassingTextChangeRange(this SourceText newText, SourceText oldText)
    {
        var ranges = newText.GetChangeRanges(oldText);
        if (ranges.Count == 0)
        {
            return default;
        }

        // simple case.
        if (ranges.Count == 1)
        {
            return ranges[0];
        }

        return TextChangeRange.Collapse(ranges);
    }

    public static int IndexOf(this SourceText text, string value, int startIndex, bool caseSensitive)
    {
        var length = text.Length - value.Length;
        var normalized = caseSensitive ? value : CaseInsensitiveComparison.ToLower(value);

        for (var i = startIndex; i <= length; i++)
        {
            var match = true;
            for (var j = 0; j < normalized.Length; j++)
            {
                // just use indexer of source text. perf of indexer depends on actual implementation of SourceText.
                // * all of our implementation at editor layer should provide either O(1) or O(log n).
                //
                // only one implementation we have that could have bad indexer perf is CompositeText with heavily modified text
                // at compiler layer but I believe that being used in find all reference will be very rare if not none.
                if (!Match(normalized[j], text[i + j], caseSensitive))
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    public static int LastIndexOf(this SourceText text, string value, int startIndex, bool caseSensitive)
    {
        var normalized = caseSensitive ? value : CaseInsensitiveComparison.ToLower(value);
        startIndex = startIndex + normalized.Length > text.Length
            ? text.Length - normalized.Length
            : startIndex;

        for (var i = startIndex; i >= 0; i--)
        {
            var match = true;
            for (var j = 0; j < normalized.Length; j++)
            {
                // just use indexer of source text. perf of indexer depends on actual implementation of SourceText.
                // * all of our implementation at editor layer should provide either O(1) or O(log n).
                //
                // only one implementation we have that could have bad indexer perf is CompositeText with heavily modified text
                // at compiler layer but I believe that being used in find all reference will be very rare if not none.
                if (!Match(normalized[j], text[i + j], caseSensitive))
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool Match(char normalizedLeft, char right, bool caseSensitive)
        => caseSensitive ? normalizedLeft == right : normalizedLeft == CaseInsensitiveComparison.ToLower(right);

    public static bool ContentEquals(this SourceText text, int position, string value)
    {
        if (position + value.Length > text.Length)
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (text[position + i] != value[i])
            {
                return false;
            }
        }

        return true;
    }

    public static int IndexOfNonWhiteSpace(this SourceText text, int start, int length)
    {
        for (var i = 0; i < length; i++)
        {
            if (!char.IsWhiteSpace(text[start + i]))
            {
                return start + i;
            }
        }

        return -1;
    }

    // 32KB. comes from SourceText char buffer size and less than large object size
    internal const int SourceTextLengthThreshold = 32 * 1024 / sizeof(char);

    public static void WriteTo(this SourceText sourceText, ObjectWriter writer, CancellationToken cancellationToken)
    {
        // Source length
        var length = sourceText.Length;
        writer.WriteInt32(length);

        // if source is small, no point on optimizing. just write out string
        if (length < SourceTextLengthThreshold)
        {
            writer.WriteString(sourceText.ToString());
        }
        else
        {
            // if bigger, write out as chunks
            WriteChunksTo(sourceText, writer, length, cancellationToken);
        }
    }

    private static void WriteChunksTo(SourceText sourceText, ObjectWriter writer, int length, CancellationToken cancellationToken)
    {
        // chunk size
        using var pooledObject = s_charArrayPool.GetPooledObject();
        var buffer = pooledObject.Object;
        Contract.ThrowIfTrue(buffer.Length != CharArrayLength);

        // We write out the chunk size for sanity purposes.
        writer.WriteInt32(CharArrayLength);

        // number of chunks
        var numberOfChunks = 1 + (length / buffer.Length);
        writer.WriteInt32(numberOfChunks);

        // write whole chunks
        var offset = 0;
        for (var i = 0; i < numberOfChunks; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var count = Math.Min(buffer.Length, length - offset);
            sourceText.CopyTo(offset, buffer, 0, count);

            // In the case where the array is entirely full, we can pass that as is to the ObjectWriter.  It already
            // supports sending the array all the way through to the underlying stream without allocations. In the case
            // where it's partially full, we pass in a span to the section that is filled.  This will fast path on
            // netcore, though will incur a copy to pooled memory on netfx.
            if (count == buffer.Length)
                writer.WriteCharArray(buffer);
            else
                writer.WriteSpan(buffer.AsSpan()[..count]);

            offset += count;
        }

        Contract.ThrowIfFalse(offset == length);
    }

    public static SourceText ReadFrom(ITextFactoryService textService, ObjectReader reader, Encoding? encoding, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
    {
        using var textReader = ObjectReaderTextReader.Create(reader);

        return textService.CreateText(textReader, encoding, checksumAlgorithm, cancellationToken);
    }

    private sealed class ObjectReaderTextReader : TextReaderWithLength
    {
        private readonly ImmutableArray<char[]> _chunks;
        private readonly int _chunkSize;
        private bool _disposed;

        private int _position;

        public static TextReader Create(ObjectReader reader)
        {
            var length = reader.ReadInt32();
            if (length < SourceTextLengthThreshold)
            {
                // small size, read as string
                return new StringReader(reader.ReadRequiredString());
            }

            var chunkSize = reader.ReadInt32();
            Contract.ThrowIfTrue(chunkSize != CharArrayLength);
            var numberOfChunks = reader.ReadInt32();

            // read as chunks
            using var _ = ArrayBuilder<char[]>.GetInstance(numberOfChunks, out var chunks);

            var offset = 0;
            for (var i = 0; i < numberOfChunks; i++)
            {
                // Shared pool array will be freed in the Dispose method below.
                var (currentChunk, currentChunkLength) = reader.ReadCharArray(
                    static length =>
                    {
                        Contract.ThrowIfTrue(length > CharArrayLength);
                        return s_charArrayPool.Allocate();
                    });

                Contract.ThrowIfTrue(currentChunk.Length != CharArrayLength);

                // All but the last chunk must be completely filled.
                Contract.ThrowIfTrue(i < numberOfChunks - 1 && currentChunkLength != CharArrayLength);

                chunks.Add(currentChunk);
                offset += currentChunkLength;
            }

            Contract.ThrowIfFalse(offset == length);
            return new ObjectReaderTextReader(chunks.ToImmutableAndClear(), chunkSize, length);
        }

        private ObjectReaderTextReader(ImmutableArray<char[]> chunks, int chunkSize, int length)
            : base(length)
        {
            _chunks = chunks;
            _chunkSize = chunkSize;
            _disposed = false;
            Contract.ThrowIfTrue(chunkSize != CharArrayLength);
            Contract.ThrowIfTrue(chunks.Any(static (c, s) => c.Length != s, chunkSize));
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                // Constructor already validated that all chunks are the right size to go back into the pool.
                foreach (var chunk in _chunks)
                    s_charArrayPool.Free(chunk);
            }

            base.Dispose(disposing);
        }

        public override int Peek()
        {
            if (_position >= Length)
            {
                return -1;
            }

            return Read(_position);
        }

        public override int Read()
        {
            if (_position >= Length)
            {
                return -1;
            }

            return Read(_position++);
        }

        public override int Read(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (index < 0 || index >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (count < 0 || (index + count) > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // check quick bail out
            if (count == 0)
            {
                return 0;
            }

            // adjust to actual char to read
            var totalCharsToRead = Math.Min(count, Length - _position);
            count = totalCharsToRead;

            var chunkIndex = GetIndexFromPosition(_position);
            var chunkStartOffset = GetColumnFromPosition(_position);

            while (true)
            {
                var chunk = _chunks[chunkIndex];
                var charsToCopy = Math.Min(chunk.Length - chunkStartOffset, count);

                Array.Copy(chunk, chunkStartOffset, buffer, index, charsToCopy);
                count -= charsToCopy;

                if (count <= 0)
                {
                    break;
                }

                index += charsToCopy;
                chunkStartOffset = 0;
                chunkIndex++;
            }

            _position += totalCharsToRead;
            return totalCharsToRead;
        }

        private int Read(int position)
        {
            var chunkIndex = GetIndexFromPosition(position);
            var chunkColumn = GetColumnFromPosition(position);

            return _chunks[chunkIndex][chunkColumn];
        }

        private int GetIndexFromPosition(int position) => position / _chunkSize;
        private int GetColumnFromPosition(int position) => position % _chunkSize;
    }
}
