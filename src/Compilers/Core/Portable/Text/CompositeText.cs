// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
            Debug.Assert(segments.Length > 0);

            _segments = segments;
            _encoding = encoding;

            ComputeLengthAndStorageSize(segments, out _length, out _storageSize);

            _segmentOffsets = new int[segments.Length];
            int offset = 0;
            for (int i = 0; i < _segmentOffsets.Length; i++)
            {
                _segmentOffsets[i] = offset;
                Debug.Assert(_segments[i].Length > 0);
                offset += _segments[i].Length;
            }
        }

        protected override TextLineCollection GetLinesCore()
            => new CompositeTextLineInfo(this);

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

            RemoveSplitLineBreaksAndEmptySegments(segments);

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

        private static void RemoveSplitLineBreaksAndEmptySegments(ArrayBuilder<SourceText> segments)
        {
            if (segments.Count > 1)
            {
                // Remove empty segments before checking for split line breaks
                segments.RemoveWhere(static (s, _, _) => s.Length == 0, default(VoidResult));

                var splitLineBreakFound = false;
                for (int i = 1; i < segments.Count; i++)
                {
                    var prevSegment = segments[i - 1];
                    var curSegment = segments[i];
                    if (prevSegment.Length > 0 && prevSegment[^1] == '\r' && curSegment[0] == '\n')
                    {
                        splitLineBreakFound = true;

                        segments[i - 1] = prevSegment.GetSubText(new TextSpan(0, prevSegment.Length - 1));
                        segments.Insert(i, SourceText.From("\r\n"));
                        segments[i + 1] = curSegment.GetSubText(new TextSpan(1, curSegment.Length - 1));
                        i++;
                    }
                }

                if (splitLineBreakFound)
                {
                    // If a split line break was present, ensure there aren't any empty lines again
                    // due to the sourcetexts created from the GetSubText calls.
                    segments.RemoveWhere(static (s, _, _) => s.Length == 0, default(VoidResult));
                }
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
        private sealed class CompositeTextLineInfo : TextLineCollection
        {
            private readonly CompositeText _compositeText;

            /// <summary>
            /// The starting line number for the correspondingly indexed SourceTexts in _compositeText.Segments.
            /// Multiple consecutive entries could indicate the same line number if the corresponding
            /// segments don't contain newline characters. 
            /// </summary>
            /// <remarks>
            /// This will be of the same length as _compositeText.Segments
            /// </remarks>
            private readonly ImmutableArray<int> _segmentLineNumbers;

            // The total number of lines in our _compositeText
            private readonly int _lineCount;

            public CompositeTextLineInfo(CompositeText compositeText)
            {
                var segmentLineNumbers = new int[compositeText.Segments.Length];
                var accumulatedLineCount = 0;

                Debug.Assert(compositeText.Segments.Length > 0);
                for (int i = 0; i < compositeText.Segments.Length; i++)
                {
                    segmentLineNumbers[i] = accumulatedLineCount;

                    var segment = compositeText.Segments[i];

                    // Account for this segments lines in our accumulated lines. Subtract one as each segment
                    // views it's line count as one greater than the number of line breaks it contains.
                    accumulatedLineCount += (segment.Lines.Count - 1);

                    Debug.Assert(segment.Length > 0);

                    // RemoveSplitLineBreaksAndEmptySegments ensured no split line breaks
                    Debug.Assert(i == compositeText.Segments.Length - 1 || segment[^1] != '\r' || compositeText.Segments[i + 1][0] != '\n');
                }

                _compositeText = compositeText;
                _segmentLineNumbers = ImmutableCollectionsMarshal.AsImmutableArray(segmentLineNumbers);

                // Add one to the accumulatedLineCount for our stored line count (so that we maintain the 
                // invariant that a text's line count is one greater than the number of newlines it contains)
                _lineCount = accumulatedLineCount + 1;
            }

            public override int Count => _lineCount;

            /// <summary>
            /// Determines the line number of a position in this CompositeText
            /// </summary>
            public override int IndexOf(int position)
            {
                if (position < 0 || position > _compositeText.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(position));
                }

                _compositeText.GetIndexAndOffset(position, out var segmentIndex, out var segmentOffset);

                var segment = _compositeText.Segments[segmentIndex];
                var lineNumberWithinSegment = segment.Lines.IndexOf(segmentOffset);

                return _segmentLineNumbers[segmentIndex] + lineNumberWithinSegment;
            }

            public override TextLine this[int lineNumber]
            {
                get
                {
                    if (lineNumber < 0 || lineNumber >= _lineCount)
                    {
                        throw new ArgumentOutOfRangeException(nameof(lineNumber));
                    }

                    // Determine the indices for segments that contribute to our view of the requested line's contents
                    GetSegmentIndexRangeContainingLine(lineNumber, out var firstSegmentIndexInclusive, out var lastSegmentIndexInclusive);
                    Debug.Assert(firstSegmentIndexInclusive <= lastSegmentIndexInclusive);

                    var firstSegmentFirstLineNumber = _segmentLineNumbers[firstSegmentIndexInclusive];
                    var firstSegment = _compositeText.Segments[firstSegmentIndexInclusive];
                    var firstSegmentOffset = _compositeText._segmentOffsets[firstSegmentIndexInclusive];
                    var firstSegmentTextLine = firstSegment.Lines[lineNumber - firstSegmentFirstLineNumber];

                    var lineLength = firstSegmentTextLine.SpanIncludingLineBreak.Length;

                    // walk forward through segments between firstSegmentIndexInclusive and lastSegmentIndexInclusive, and add their
                    // view of the length of this line. This loop handles all segments between firstSegmentIndexInclusive and lastSegmentIndexInclusive.
                    for (var nextSegmentIndex = firstSegmentIndexInclusive + 1; nextSegmentIndex < lastSegmentIndexInclusive; nextSegmentIndex++)
                    {
                        var nextSegment = _compositeText.Segments[nextSegmentIndex];

                        // Segments between firstSegmentIndexInclusive and lastSegmentIndexInclusive should have either exactly one line or
                        // exactly two lines and the second line being empty.
                        Debug.Assert((nextSegment.Lines.Count == 1) ||
                                     (nextSegment.Lines.Count == 2 && nextSegment.Lines[1].SpanIncludingLineBreak.IsEmpty));

                        lineLength += nextSegment.Lines[0].SpanIncludingLineBreak.Length;
                    }

                    if (firstSegmentIndexInclusive != lastSegmentIndexInclusive)
                    {
                        var lastSegment = _compositeText.Segments[lastSegmentIndexInclusive];

                        // lastSegment should have at least one line.
                        Debug.Assert(lastSegment.Lines.Count >= 1);

                        lineLength += lastSegment.Lines[0].SpanIncludingLineBreak.Length;
                    }

                    var resultLine = TextLine.FromSpanUnsafe(_compositeText, new TextSpan(firstSegmentOffset + firstSegmentTextLine.Start, lineLength));

                    // Assert resultLine only has line breaks in the appropriate locations
                    Debug.Assert(resultLine.ToString().All(static c => !TextUtilities.IsAnyLineBreakCharacter(c)));

                    return resultLine;
                }
            }

            private void GetSegmentIndexRangeContainingLine(int lineNumber, out int firstSegmentIndexInclusive, out int lastSegmentIndexInclusive)
            {
                var idx = _segmentLineNumbers.BinarySearch(lineNumber);
                var binarySearchSegmentIndex = idx >= 0 ? idx : (~idx - 1);

                // Walk backwards starting at binarySearchSegmentIndex to find the earliest segment index that intersects this line number
                for (firstSegmentIndexInclusive = binarySearchSegmentIndex; firstSegmentIndexInclusive > 0; firstSegmentIndexInclusive--)
                {
                    if (_segmentLineNumbers[firstSegmentIndexInclusive] != lineNumber)
                    {
                        // This segment doesn't start at the requested line, no need to continue to earlier segments.
                        break;
                    }

                    // No need to include the previous segment if it ends in a newline character
                    var previousSegment = _compositeText.Segments[firstSegmentIndexInclusive - 1];
                    var previousSegmentLastChar = previousSegment[^1];
                    if (TextUtilities.IsAnyLineBreakCharacter(previousSegmentLastChar))
                    {
                        break;
                    }
                }

                for (lastSegmentIndexInclusive = binarySearchSegmentIndex; lastSegmentIndexInclusive < _compositeText.Segments.Length - 1; lastSegmentIndexInclusive++)
                {
                    if (_segmentLineNumbers[lastSegmentIndexInclusive + 1] != lineNumber)
                    {
                        break;
                    }
                }
            }
        }
    }
}
