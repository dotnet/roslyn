// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        private readonly int _size;
        private readonly Encoding _encoding;

        private CompositeText(ImmutableArray<SourceText> segments, Encoding encoding, SourceHashAlgorithm checksumAlgorithm)
            : base(checksumAlgorithm: checksumAlgorithm)
        {
            Debug.Assert(!segments.IsDefaultOrEmpty);

            _segments = segments;
            _encoding = encoding;

            ComputeLengthAndSize(segments, out _length, out _size);
        }

        public override Encoding Encoding
        {
            get { return _encoding; }
        }

        public override int Length
        {
            get { return _length; }
        }

        internal override int Size
        {
            get { return _size; }
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
            while (segIndex < _segments.Length && count > 0)
            {
                var segment = _segments[segIndex];
                var copyLength = Math.Min(count, segment.Length - segOffset);

                AddSegments(newSegments, segment.GetSubText(new TextSpan(segOffset, copyLength)));

                count -= copyLength;
                segIndex++;
                segOffset = 0;
            }

            return ToSourceTextAndFree(newSegments, this, adjustSegments: false);
        }

        private void GetIndexAndOffset(int position, out int index, out int offset)
        {
#if false
            // TODO: not sure if this is necessary if number of segments is constrained.

            // Binary search to find the chunk that contains the given position.
            int idx = _segmentOffsets.BinarySearch(position);
            index = idx >= 0 ? idx : (~idx - 1);
            offset = position - _segmentOffsets[index];

#else
            for (int i = 0; i < _segments.Length; i++)
            {
                var segment = _segments[i];
                if (position < segment.Length)
                {
                    index = i;
                    offset = position;
                    return;
                }
                else
                {
                    position -= segment.Length;
                }
            }

            index = 0;
            offset = 0;
            throw new ArgumentException("position");
#endif
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
            CompositeText composite = text as CompositeText;
            if (composite == null)
            {
                segments.Add(text);
            }
            else
            {
                segments.AddRange(composite._segments);
            }
        }

        internal static SourceText ToSourceTextAndFree(ArrayBuilder<SourceText> segments, SourceText original, bool adjustSegments)
        {
            if (adjustSegments)
            {
                TrimInaccessibleText(segments);
                ReduceSegmentCount(segments);
            }

            if (segments.Count == 0)
            {
                segments.Free();
                return SourceText.From(string.Empty, original.Encoding, original.ChecksumAlgorithm);
            }
            else if (segments.Count == 1)
            {
                SourceText result = segments[0];
                segments.Free();
                return result;
            }
            else
            {
                return new CompositeText(segments.ToImmutableAndFree(), original.Encoding, original.ChecksumAlgorithm);
            }
        }

        internal const int IDEAL_SEGMENT_COUNT = 10;
        internal const int MAXIMUM_SEGMENT_COUNT = 20;

        /// <summary>
        /// Reduces the number of segments toward the ideal number of segments.
        /// </summary>
        private static void ReduceSegmentCount(ArrayBuilder<SourceText> segments)
        {
            if (segments.Count > MAXIMUM_SEGMENT_COUNT)
            {
                var minimumSegmentSize = GetMinimalSegmentSize(segments);
                if (minimumSegmentSize > 0)
                {
                    CombineSegments(segments, minimumSegmentSize);
                }
            }
        }

        private const int INITIAL_SEGMENT_SIZE = 32;        // allow combining if each text segment size is less than this
        private const int MAXIMUM_SEGMENT_SIZE = int.MaxValue / 16; // give up on reducing if segment sizes are all greater than this

        /// <summary>
        /// Gets the minimal segment size used to determine which segments are eligible to be combined with their neighbors.
        /// </summary>
        private static int GetMinimalSegmentSize(ArrayBuilder<SourceText> segments)
        {
            // find the minimal segment size that reduces enough segments to less that or equal to the ideal segment count
            for (var minimumSegmentSize = INITIAL_SEGMENT_SIZE;
                minimumSegmentSize <= MAXIMUM_SEGMENT_SIZE;
                minimumSegmentSize *= 2)
            {
                if (GetReducedSize(segments, minimumSegmentSize) <= IDEAL_SEGMENT_COUNT)
                {
                    return minimumSegmentSize;
                }
            }

            // cannot find an appropriate size for segments to be combined.
            return -1;
        }

        /// <summary>
        /// Combines continguous segments with lengths that are each less than or equal to the minimum segment size.
        /// </summary>
        private static void CombineSegments(ArrayBuilder<SourceText> segments, int minimumSegmentSize)
        {
            for (int i = 0; i < segments.Count - 1; i++)
            {
                if (segments[i].Length <= minimumSegmentSize)
                {
                    int combinedLength = segments[i].Length;

                    // count how many contiguous segments are reducible
                    int count = 1;
                    for (int j = i + 1; j < segments.Count; j++)
                    {
                        if (segments[j].Length <= minimumSegmentSize)
                        {
                            count++;
                            combinedLength += segments[j].Length;
                        }
                    }

                    // if we've got at least two, then combine them into a single text
                    if (count > 1)
                    {
                        var encoding = segments[i].Encoding;
                        var algorithm = segments[i].ChecksumAlgorithm;

                        var writer = SourceTextWriter.Create(encoding, algorithm, combinedLength);

                        while (count > 0)
                        {
                            segments[i].Write(writer);
                            segments.RemoveAt(i);
                            count--;
                        }

                        var newText = writer.ToSourceText();

                        segments.Insert(i, newText);
                    }
                }
            }
        }

        /// <summary>
        /// Determines the total number of segments that would remain if the contiguous segments of length less than or equal to
        /// the minimum segment size are combined together.
        /// </summary>
        private static int GetReducedSize(ArrayBuilder<SourceText> segments, int minimumSegmentSize)
        {
            int numberOfSegmentsReduced = 0;

            for (int i = 0; i < segments.Count - 1; i++)
            {
                if (segments[i].Length <= minimumSegmentSize)
                {
                    // count how many contiguous segments are reducible
                    int count = 1;
                    for (int j = i + 1; j < segments.Count; j++)
                    {
                        if (segments[j].Length <= minimumSegmentSize)
                        {
                            count++;
                        }
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

        private static ObjectPool<HashSet<SourceText>> _uniqueSourcesPool
            = new ObjectPool<HashSet<SourceText>>(() => new HashSet<SourceText>(), 10);

        /// <summary>
        /// Compute total text length and total size of storage buffers held
        /// </summary>
        private static void ComputeLengthAndSize(IReadOnlyList<SourceText> segments, out int length, out int size)
        {
            var uniqueSources = _uniqueSourcesPool.Allocate();

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
                size += segment.Size;
            }

            uniqueSources.Clear();
            _uniqueSourcesPool.Free(uniqueSources);
        }

        /// <summary>
        /// Trim excessive inaccessible text.
        /// </summary>
        private static void TrimInaccessibleText(ArrayBuilder<SourceText> segments)
        {
            int length, size;
            ComputeLengthAndSize(segments, out length, out size);

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
    }
}
