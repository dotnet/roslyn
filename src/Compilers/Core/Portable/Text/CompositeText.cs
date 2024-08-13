// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// A composite of a sequence of <see cref="SourceText"/>s.
    /// </summary>
    internal sealed class CompositeText : SourceText
    {
        private readonly ImmutableArray<SourceText> _segments;
        private readonly int _length;
        private readonly int _storageSize;
        private readonly int[] _segmentOffsets;
        private readonly Encoding? _encoding;

        private CompositeText(ImmutableArray<SourceText> segments, Encoding? encoding, SourceHashAlgorithm checksumAlgorithm)
            : base(checksumAlgorithm: checksumAlgorithm)
        {
            Debug.Assert(!segments.IsDefaultOrEmpty);

            _segments = segments;
            _encoding = encoding;

            ComputeLengthAndStorageSize(segments, out _length, out _storageSize);

            _segmentOffsets = new int[segments.Length];
            int offset = 0;
            for (int i = 0; i < _segmentOffsets.Length; i++)
            {
                _segmentOffsets[i] = offset;
                offset += _segments[i].Length;
            }
        }

        protected override TextLineCollection GetLinesCore()
        {
            return new CompositeLineInfo(this);
        }

        public override Encoding? Encoding
        {
            get { return _encoding; }
        }

        public override int Length
        {
            get { return _length; }
        }

        internal override int StorageSize
        {
            get { return _storageSize; }
        }

        internal override ImmutableArray<SourceText> Segments
        {
            get { return _segments; }
        }

        public override char this[int position]
        {
            get
            {
                int index;
                int offset;
                GetIndexAndOffset(position, out index, out offset);
                return _segments[index][offset];
            }
        }

        public override SourceText GetSubText(TextSpan span)
        {
            CheckSubSpan(span);

            var sourceIndex = span.Start;
            var count = span.Length;

            int segIndex;
            int segOffset;
            GetIndexAndOffset(sourceIndex, out segIndex, out segOffset);

            var newSegments = ArrayBuilder<SourceText>.GetInstance();
            try
            {
                while (segIndex < _segments.Length && count > 0)
                {
                    var segment = _segments[segIndex];
                    var copyLength = Math.Min(count, segment.Length - segOffset);

                    AddSegments(newSegments, segment.GetSubText(new TextSpan(segOffset, copyLength)));

                    count -= copyLength;
                    segIndex++;
                    segOffset = 0;
                }

                return ToSourceText(newSegments, this, adjustSegments: false);
            }
            finally
            {
                newSegments.Free();
            }
        }

        private void GetIndexAndOffset(int position, out int index, out int offset)
        {
            // Binary search to find the chunk that contains the given position.
            int idx = _segmentOffsets.BinarySearch(position);
            index = idx >= 0 ? idx : (~idx - 1);
            offset = position - _segmentOffsets[index];
        }

        /// <summary>
        /// Validates the arguments passed to <see cref="CopyTo"/> against the published contract.
        /// </summary>
        /// <returns>True if should bother to proceed with copying.</returns>
        private bool CheckCopyToArguments(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));

            if (destinationIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex));

            if (count < 0 || count > this.Length - sourceIndex || count > destination.Length - destinationIndex)
                throw new ArgumentOutOfRangeException(nameof(count));

            return count > 0;
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (!CheckCopyToArguments(sourceIndex, destination, destinationIndex, count))
                return;

            int segIndex;
            int segOffset;
            GetIndexAndOffset(sourceIndex, out segIndex, out segOffset);

            while (segIndex < _segments.Length && count > 0)
            {
                var segment = _segments[segIndex];
                var copyLength = Math.Min(count, segment.Length - segOffset);

                segment.CopyTo(segOffset, destination, destinationIndex, copyLength);

                count -= copyLength;
                destinationIndex += copyLength;
                segIndex++;
                segOffset = 0;
            }
        }

        internal static void AddSegments(ArrayBuilder<SourceText> segments, SourceText text)
        {
            CompositeText? composite = text as CompositeText;
            if (composite == null)
            {
                segments.Add(text);
            }
            else
            {
                segments.AddRange(composite._segments);
            }
        }

        internal static SourceText ToSourceText(ArrayBuilder<SourceText> segments, SourceText original, bool adjustSegments)
        {
            if (adjustSegments)
            {
                TrimInaccessibleText(segments);
                ReduceSegmentCountIfNecessary(segments);
            }

            if (segments.Count == 0)
            {
                return SourceText.From(string.Empty, original.Encoding, original.ChecksumAlgorithm);
            }
            else if (segments.Count == 1)
            {
                return segments[0];
            }
            else
            {
                return new CompositeText(segments.ToImmutable(), original.Encoding, original.ChecksumAlgorithm);
            }
        }

        // both of these numbers are currently arbitrary.
        internal const int TARGET_SEGMENT_COUNT_AFTER_REDUCTION = 32;
        internal const int MAXIMUM_SEGMENT_COUNT_BEFORE_REDUCTION = 64;

        /// <summary>
        /// Reduces the number of segments toward the target number of segments,
        /// if the number of segments is deemed to be too large (beyond the maximum).
        /// </summary>
        private static void ReduceSegmentCountIfNecessary(ArrayBuilder<SourceText> segments)
        {
            if (segments.Count > MAXIMUM_SEGMENT_COUNT_BEFORE_REDUCTION)
            {
                var segmentSize = GetMinimalSegmentSizeToUseForCombining(segments);
                CombineSegments(segments, segmentSize);
            }
        }

        // Allow combining segments if each has a size less than or equal to this amount.
        // This is some arbitrary number deemed to be small
        private const int INITIAL_SEGMENT_SIZE_FOR_COMBINING = 32;

        // Segments must be less than (or equal) to this size to be combined with other segments.
        // This is some arbitrary number that is a fraction of max int.
        private const int MAXIMUM_SEGMENT_SIZE_FOR_COMBINING = int.MaxValue / 16;

        /// <summary>
        /// Determines the segment size to use for call to CombineSegments, that will result in the segment count
        /// being reduced to less than or equal to the target segment count.
        /// </summary>
        private static int GetMinimalSegmentSizeToUseForCombining(ArrayBuilder<SourceText> segments)
        {
            // find the minimal segment size that reduces enough segments to less that or equal to the ideal segment count
            for (var segmentSize = INITIAL_SEGMENT_SIZE_FOR_COMBINING;
                segmentSize <= MAXIMUM_SEGMENT_SIZE_FOR_COMBINING;
                segmentSize *= 2)
            {
                if (GetSegmentCountIfCombined(segments, segmentSize) <= TARGET_SEGMENT_COUNT_AFTER_REDUCTION)
                {
                    return segmentSize;
                }
            }

            return MAXIMUM_SEGMENT_SIZE_FOR_COMBINING;
        }

        /// <summary>
        /// Determines the segment count that would result if the segments of size less than or equal to 
        /// the specified segment size were to be combined.
        /// </summary>
        private static int GetSegmentCountIfCombined(ArrayBuilder<SourceText> segments, int segmentSize)
        {
            int numberOfSegmentsReduced = 0;

            for (int i = 0; i < segments.Count - 1; i++)
            {
                if (segments[i].Length <= segmentSize)
                {
                    // count how many contiguous segments can be combined
                    int count = 1;
                    for (int j = i + 1; j < segments.Count; j++)
                    {
                        if (segments[j].Length > segmentSize)
                        {
                            break;
                        }

                        count++;
                    }

                    if (count > 1)
                    {
                        var removed = count - 1;
                        numberOfSegmentsReduced += removed;
                        i += removed;
                    }
                }
            }

            return segments.Count - numberOfSegmentsReduced;
        }

        /// <summary>
        /// Combines contiguous segments with lengths that are each less than or equal to the specified segment size.
        /// </summary>
        private static void CombineSegments(ArrayBuilder<SourceText> segments, int segmentSize)
        {
            for (int i = 0; i < segments.Count - 1; i++)
            {
                if (segments[i].Length <= segmentSize)
                {
                    int combinedLength = segments[i].Length;

                    // count how many contiguous segments are reducible
                    int count = 1;
                    for (int j = i + 1; j < segments.Count; j++)
                    {
                        if (segments[j].Length > segmentSize)
                        {
                            break;
                        }

                        count++;
                        combinedLength += segments[j].Length;
                    }

                    // if we've got at least two, then combine them into a single text
                    if (count > 1)
                    {
                        var encoding = segments[i].Encoding;
                        var algorithm = segments[i].ChecksumAlgorithm;

                        var writer = SourceTextWriter.Create(encoding, algorithm, combinedLength);

                        for (int j = i; j < i + count; j++)
                            segments[j].Write(writer);

                        var newText = writer.ToSourceText();

                        segments.RemoveRange(i, count);
                        segments.Insert(i, newText);
                    }
                }
            }
        }

        private static readonly ObjectPool<HashSet<SourceText>> s_uniqueSourcesPool
            = new ObjectPool<HashSet<SourceText>>(() => new HashSet<SourceText>(), 5);

        /// <summary>
        /// Compute total text length and total size of storage buffers held
        /// </summary>
        private static void ComputeLengthAndStorageSize(IReadOnlyList<SourceText> segments, out int length, out int size)
        {
            var uniqueSources = s_uniqueSourcesPool.Allocate();

            length = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                length += segment.Length;
                uniqueSources.Add(segment.StorageKey);
            }

            size = 0;
            foreach (var segment in uniqueSources)
            {
                size += segment.StorageSize;
            }

            uniqueSources.Clear();
            s_uniqueSourcesPool.Free(uniqueSources);
        }

        /// <summary>
        /// Trim excessive inaccessible text.
        /// </summary>
        private static void TrimInaccessibleText(ArrayBuilder<SourceText> segments)
        {
            int length, size;
            ComputeLengthAndStorageSize(segments, out length, out size);

            // if more than half of the storage is unused, compress into a single new segment
            if (length < size / 2)
            {
                var encoding = segments[0].Encoding;
                var algorithm = segments[0].ChecksumAlgorithm;

                var writer = SourceTextWriter.Create(encoding, algorithm, length);
                foreach (var segment in segments)
                {
                    segment.Write(writer);
                }

                segments.Clear();
                segments.Add(writer.ToSourceText());
            }
        }

        /// <summary>
        /// Delegates to SourceTexts within the CompositeText to determine line information.
        /// </summary>
        internal sealed class CompositeLineInfo : TextLineCollection
        {
            private readonly CompositeText _compositeText;
            private readonly int[] _segmentLineIndexes;
            private readonly int _lineCount;

            public CompositeLineInfo(CompositeText compositeText)
            {
                _compositeText = compositeText;
                _segmentLineIndexes = new int[compositeText.Segments.Length];

                for (int i = 0; i < compositeText.Segments.Length; i++)
                {
                    _segmentLineIndexes[i] = _lineCount;

                    var segment = compositeText.Segments[i];
                    _lineCount += (segment.Lines.Count - 1);

                    // If "\r\n" is split amongst adjacent segments, reduce the line count by one as both segments
                    // would count their corresponding CR or LF as a newline.
                    if (segment.Length > 0 &&
                        segment[segment.Length - 1] == '\r' &&
                        i < compositeText.Segments.Length - 1)
                    {
                        var nextSegment = compositeText.Segments[i + 1];
                        if (nextSegment.Length > 0 && nextSegment[0] == '\n')
                        {
                            _lineCount -= 1;
                        }
                    }
                }

                _lineCount += 1;
            }

            public override int Count => _lineCount;

            public override int IndexOf(int position)
            {
                _compositeText.GetIndexAndOffset(position, out var segmentIndex, out var segmentOffset);

                var segment = _compositeText.Segments[segmentIndex];
                var lineIndexWithinSegment = segment.Lines.IndexOf(segmentOffset);

                return _segmentLineIndexes[segmentIndex] + lineIndexWithinSegment;
            }

            public override TextLine this[int lineNumber]
            {
                get
                {
                    // Determine the indexes for segments that contribute to our view of this line's contents
                    GetSegmentIndexRangeContainingLine(lineNumber, out var firstSegmentIndex, out var lastSegmentIndex);

                    var firstSegmentFirstLineNumber = _segmentLineIndexes[firstSegmentIndex];
                    var firstSegment = _compositeText.Segments[firstSegmentIndex];
                    var firstSegmentOffset = _compositeText._segmentOffsets[firstSegmentIndex];
                    var firstSegmentTextLine = firstSegment.Lines[lineNumber - firstSegmentFirstLineNumber];

                    var lineLength = firstSegmentTextLine.SpanIncludingLineBreak.Length;

                    // walk forward through remaining segments contributing to this line, and add their
                    // view of this line.
                    for (var nextSegmentIndex = firstSegmentIndex + 1; nextSegmentIndex <= lastSegmentIndex; nextSegmentIndex++)
                    {
                        var nextSegment = _compositeText.Segments[nextSegmentIndex];
                        lineLength += nextSegment.Lines[0].SpanIncludingLineBreak.Length;
                    }

                    return TextLine.FromSpanUnsafe(_compositeText, new TextSpan(firstSegmentOffset + firstSegmentTextLine.Start, lineLength));
                }
            }

            private void GetSegmentIndexRangeContainingLine(int lineNumber, out int firstSegmentIndex, out int lastSegmentIndex)
            {
                int idx = _segmentLineIndexes.BinarySearch(lineNumber);
                var binarySearchSegmentIndex = idx >= 0 ? idx : (~idx - 1);

                for (firstSegmentIndex = binarySearchSegmentIndex; firstSegmentIndex > 0; firstSegmentIndex--)
                {
                    if (_segmentLineIndexes[firstSegmentIndex] != lineNumber)
                    {
                        // This segment doesn't start at the requested line, no need to continue to earlier segments.
                        break;
                    }

                    // No need to continue to the previous segment if either:
                    // 1) it ends in \n or
                    // 2) if ends in \r and the current segment doesn't start with \n
                    var previousSegment = _compositeText.Segments[firstSegmentIndex - 1];
                    var previousSegmentLastChar = previousSegment.Length > 0 ? previousSegment[previousSegment.Length - 1] : '\0';
                    if (previousSegmentLastChar == '\n')
                    {
                        break;
                    }
                    else if (previousSegmentLastChar == '\r')
                    {
                        var currentSegment = _compositeText.Segments[firstSegmentIndex];
                        if (currentSegment.Length == 0 || currentSegment[0] != '\n')
                        {
                            break;
                        }
                    }
                }

                // Determining the lastSegment is a simple walk as the _segmentLineIndexes was populated
                // accounting for split "\r\n".
                for (lastSegmentIndex = binarySearchSegmentIndex; lastSegmentIndex < _compositeText.Segments.Length - 1; lastSegmentIndex++)
                {
                    if (_segmentLineIndexes[lastSegmentIndex + 1] != lineNumber)
                    {
                        break;
                    }
                }
            }
        }
    }
}
