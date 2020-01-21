// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class SourceTextExtensions
    {
        /// <summary>
        /// Returns the leading whitespace of the line located at the specified position in the given snapshot.
        /// </summary>
        public static string GetLeadingWhitespaceOfLineAtPosition(this SourceText text, int position)
        {
            Contract.ThrowIfNull(text);

            var line = text.Lines.GetLineFromPosition(position);
            var linePosition = line.GetFirstNonWhitespacePosition();
            if (!linePosition.HasValue)
            {
                return line.ToString();
            }

            var lineText = line.ToString();
            return lineText.Substring(0, linePosition.Value - line.Start);
        }

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
                    // * all of our implementation at editor layer should provide either O(1) or O(logn).
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
                    // * all of our implementation at editor layer should provide either O(1) or O(logn).
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
        {
            return caseSensitive ? normalizedLeft == right : normalizedLeft == CaseInsensitiveComparison.ToLower(right);
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
            var buffer = SharedPools.CharArray.Allocate();
            writer.WriteInt32(buffer.Length);

            // number of chunks
            var numberOfChunks = 1 + (length / buffer.Length);
            writer.WriteInt32(numberOfChunks);

            // write whole chunks
            try
            {
                var offset = 0;
                for (var i = 0; i < numberOfChunks; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var count = Math.Min(buffer.Length, length - offset);
                    if ((i < numberOfChunks - 1) || (count == buffer.Length))
                    {
                        // chunks before last chunk or last chunk match buffer size
                        sourceText.CopyTo(offset, buffer, 0, buffer.Length);
                        writer.WriteValue(buffer);
                    }
                    else if (i == numberOfChunks - 1)
                    {
                        // last chunk which size is not buffer size
                        var tempArray = new char[count];

                        sourceText.CopyTo(offset, tempArray, 0, tempArray.Length);
                        writer.WriteValue(tempArray);
                    }

                    offset += count;
                }

                Contract.ThrowIfFalse(offset == length);
            }
            finally
            {
                SharedPools.CharArray.Free(buffer);
            }
        }

        public static SourceText ReadFrom(ITextFactoryService textService, ObjectReader reader, Encoding encoding, CancellationToken cancellationToken)
        {
            using var textReader = ObjectReaderTextReader.Create(reader);

            return textService.CreateText(textReader, encoding, cancellationToken);
        }

        private class ObjectReaderTextReader : TextReaderWithLength
        {
            private readonly ImmutableArray<char[]> _chunks;
            private readonly int _chunkSize;

            private int _position;

            public static TextReader Create(ObjectReader reader)
            {
                var length = reader.ReadInt32();
                if (length < SourceTextLengthThreshold)
                {
                    // small size, read as string
                    return new StringReader(reader.ReadString());
                }

                // read as chunks
                var builder = ImmutableArray.CreateBuilder<char[]>();

                var chunkSize = reader.ReadInt32();
                var numberOfChunks = reader.ReadInt32();

                var offset = 0;
                for (var i = 0; i < numberOfChunks; i++)
                {
                    var currentLine = (char[])reader.ReadValue();
                    builder.Add(currentLine);

                    offset += currentLine.Length;
                }

                Contract.ThrowIfFalse(offset == length);
                return new ObjectReaderTextReader(builder.ToImmutable(), chunkSize, length);
            }

            private ObjectReaderTextReader(ImmutableArray<char[]> chunks, int chunkSize, int length)
                : base(length)
            {
                _chunks = chunks;
                _chunkSize = chunkSize;
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
                var totalCharsToRead = Math.Min(count, (int)(Length - _position));
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
}
