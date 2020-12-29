// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections.Internal;

namespace Microsoft.CodeAnalysis.Collections
{
    internal static class SegmentedArray
    {
        /// <seealso cref="Array.Clear(Array, int, int)"/>
        internal static void Clear<T>(SegmentedArray<T> array, int index, int length)
        {
            foreach (var memory in array.GetSegments(index, length))
            {
                memory.Span.Clear();
            }
        }

        /// <seealso cref="Array.Copy(Array, Array, int)"/>
        internal static void Copy<T>(SegmentedArray<T> sourceArray, SegmentedArray<T> destinationArray, int length)
        {
            if (length == 0)
                return;

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (length > sourceArray.Length)
                throw new ArgumentException(SR.Arg_LongerThanSrcArray, nameof(sourceArray));
            if (length > destinationArray.Length)
                throw new ArgumentException(SR.Arg_LongerThanDestArray, nameof(destinationArray));

            foreach (var (first, second) in GetSegments(sourceArray, destinationArray, length))
            {
                first.CopyTo(second);
            }
        }

        private static SegmentEnumerable<T> GetSegments<T>(this SegmentedArray<T> array, int offset, int length)
            => new(array, offset, length);

        private static AlignedSegmentEnumerable<T> GetSegments<T>(SegmentedArray<T> first, SegmentedArray<T> second, int length)
            => new(first, second, length);

#pragma warning disable IDE0051 // Remove unused private members (will be used in follow-up)
        private static AlignedSegmentEnumerable<T> GetSegmentsAligned<T>(SegmentedArray<T> first, int firstOffset, SegmentedArray<T> second, int secondOffset, int length)
            => new(first, firstOffset, second, secondOffset, length);

        private static UnalignedSegmentEnumerable<T> GetSegmentsUnaligned<T>(SegmentedArray<T> first, int firstOffset, SegmentedArray<T> second, int secondOffset, int length)
            => new(first, firstOffset, second, secondOffset, length);
#pragma warning restore IDE0051 // Remove unused private members

        private readonly struct AlignedSegmentEnumerable<T>
        {
            private readonly SegmentedArray<T> _first;
            private readonly int _firstOffset;
            private readonly SegmentedArray<T> _second;
            private readonly int _secondOffset;
            private readonly int _length;

            public AlignedSegmentEnumerable(SegmentedArray<T> first, SegmentedArray<T> second, int length)
                : this(first, 0, second, 0, length)
            {
            }

            public AlignedSegmentEnumerable(SegmentedArray<T> first, int firstOffset, SegmentedArray<T> second, int secondOffset, int length)
            {
                _first = first;
                _firstOffset = firstOffset;
                _second = second;
                _secondOffset = secondOffset;
                _length = length;
            }

            public AlignedSegmentEnumerator<T> GetEnumerator()
                => new((T[][])_first.SyncRoot, _firstOffset, (T[][])_second.SyncRoot, _secondOffset, _length);
        }

        private struct AlignedSegmentEnumerator<T>
        {
            private readonly T[][] _firstSegments;
            private readonly int _firstOffset;
            private readonly T[][] _secondSegments;
            private readonly int _secondOffset;
            private readonly int _length;

            private int _completed;
            private (Memory<T> first, Memory<T> second) _current;

            public AlignedSegmentEnumerator(T[][] firstSegments, int firstOffset, T[][] secondSegments, int secondOffset, int length)
            {
                _firstSegments = firstSegments;
                _firstOffset = firstOffset;
                _secondSegments = secondSegments;
                _secondOffset = secondOffset;
                _length = length;

                _completed = 0;
                _current = (Memory<T>.Empty, Memory<T>.Empty);
            }

            public (Memory<T> first, Memory<T> second) Current => _current;

            public bool MoveNext()
            {
                if (_completed == _length)
                {
                    _current = (Memory<T>.Empty, Memory<T>.Empty);
                    return false;
                }

                var segmentLength = _firstSegments[0].Length;
                if (_completed == 0)
                {
                    var initialFirstSegment = _firstOffset / segmentLength;
                    var initialFirstSegmentStart = initialFirstSegment * segmentLength;
                    var initialSecondSegment = _secondOffset / segmentLength;
                    var initialSecondSegmentStart = initialSecondSegment * segmentLength;
                    var offset = _firstOffset - initialFirstSegmentStart;
                    Debug.Assert(offset == (_secondOffset - initialSecondSegmentStart), "Aligned views must start at the same segment offset");

                    var firstSegment = _firstSegments[initialFirstSegment];
                    var secondSegment = _secondSegments[initialSecondSegment];
                    var remainingInSegment = firstSegment.Length - offset;
                    var currentSegmentLength = Math.Min(remainingInSegment, _length);
                    _current = (firstSegment.AsMemory().Slice(offset, currentSegmentLength), secondSegment.AsMemory().Slice(offset, currentSegmentLength));
                    _completed = currentSegmentLength;
                    return true;
                }
                else
                {
                    var firstSegment = _firstSegments[(_completed + _firstOffset) / segmentLength];
                    var secondSegment = _secondSegments[(_completed + _secondOffset) / segmentLength];
                    var currentSegmentLength = Math.Min(segmentLength, _length - _completed);
                    _current = (firstSegment.AsMemory().Slice(0, currentSegmentLength), secondSegment.AsMemory().Slice(0, currentSegmentLength));
                    _completed += currentSegmentLength;
                    return true;
                }
            }
        }

        private readonly struct UnalignedSegmentEnumerable<T>
        {
            private readonly SegmentedArray<T> _first;
            private readonly int _firstOffset;
            private readonly SegmentedArray<T> _second;
            private readonly int _secondOffset;
            private readonly int _length;

            public UnalignedSegmentEnumerable(SegmentedArray<T> first, SegmentedArray<T> second, int length)
                : this(first, 0, second, 0, length)
            {
            }

            public UnalignedSegmentEnumerable(SegmentedArray<T> first, int firstOffset, SegmentedArray<T> second, int secondOffset, int length)
            {
                _first = first;
                _firstOffset = firstOffset;
                _second = second;
                _secondOffset = secondOffset;
                _length = length;
            }

            public UnalignedSegmentEnumerator<T> GetEnumerator()
                => new((T[][])_first.SyncRoot, _firstOffset, (T[][])_second.SyncRoot, _secondOffset, _length);
        }

        private struct UnalignedSegmentEnumerator<T>
        {
            private readonly T[][] _firstSegments;
            private readonly int _firstOffset;
            private readonly T[][] _secondSegments;
            private readonly int _secondOffset;
            private readonly int _length;

            private int _completed;
            private (Memory<T> first, Memory<T> second) _current;

            public UnalignedSegmentEnumerator(T[][] firstSegments, int firstOffset, T[][] secondSegments, int secondOffset, int length)
            {
                _firstSegments = firstSegments;
                _firstOffset = firstOffset;
                _secondSegments = secondSegments;
                _secondOffset = secondOffset;
                _length = length;

                _completed = 0;
                _current = (Memory<T>.Empty, Memory<T>.Empty);
            }

            public (Memory<T> first, Memory<T> second) Current => _current;

            public bool MoveNext()
            {
                if (_completed == _length)
                {
                    _current = (Memory<T>.Empty, Memory<T>.Empty);
                    return false;
                }

                var segmentLength = Math.Max(_firstSegments[0].Length, _secondSegments[0].Length);
                var initialFirstSegment = (_completed + _firstOffset) / segmentLength;
                var initialFirstSegmentStart = initialFirstSegment * segmentLength;
                var initialSecondSegment = (_completed + _secondOffset) / segmentLength;
                var initialSecondSegmentStart = initialSecondSegment * segmentLength;
                var firstOffset = _completed + _firstOffset - initialFirstSegmentStart;
                var secondOffset = _completed + _secondOffset - initialSecondSegmentStart;

                var firstSegment = _firstSegments[initialFirstSegment];
                var secondSegment = _secondSegments[initialSecondSegment];
                var remainingInFirstSegment = firstSegment.Length - firstOffset;
                var remainingInSecondSegment = secondSegment.Length - secondOffset;
                var currentSegmentLength = Math.Min(Math.Min(remainingInFirstSegment, remainingInSecondSegment), _length);
                _current = (firstSegment.AsMemory().Slice(firstOffset, currentSegmentLength), secondSegment.AsMemory().Slice(secondOffset, currentSegmentLength));
                _completed += currentSegmentLength;
                return true;
            }
        }

        private readonly struct SegmentEnumerable<T>
        {
            private readonly SegmentedArray<T> _array;
            private readonly int _offset;
            private readonly int _length;

            public SegmentEnumerable(SegmentedArray<T> array)
            {
                _array = array;
                _offset = 0;
                _length = array.Length;
            }

            public SegmentEnumerable(SegmentedArray<T> array, int offset, int length)
            {
                if (offset < 0 || length < 0 || (uint)(offset + length) > (uint)array.Length)
                    ThrowHelper.ThrowArgumentOutOfRangeException();

                _array = array;
                _offset = offset;
                _length = length;
            }

            public SegmentEnumerator<T> GetEnumerator()
                => new SegmentEnumerator<T>((T[][])_array.SyncRoot, _offset, _length);
        }

        private struct SegmentEnumerator<T>
        {
            private readonly T[][] _segments;
            private readonly int _offset;
            private readonly int _length;

            private int _completed;
            private Memory<T> _current;

            public SegmentEnumerator(T[][] segments, int offset, int length)
            {
                _segments = segments;
                _offset = offset;
                _length = length;

                _completed = 0;
                _current = Memory<T>.Empty;
            }

            public Memory<T> Current => _current;

            public bool MoveNext()
            {
                if (_completed == _length)
                {
                    _current = Memory<T>.Empty;
                    return false;
                }

                var segmentLength = _segments[0].Length;
                if (_completed == 0)
                {
                    var firstSegment = _offset / segmentLength;
                    var firstSegmentStart = firstSegment * segmentLength;
                    var offset = _offset - firstSegmentStart;

                    var segment = _segments[firstSegment];
                    var remainingInSegment = segment.Length - offset;
                    _current = segment.AsMemory().Slice(offset, Math.Min(remainingInSegment, _length));
                    _completed = _current.Length;
                    return true;
                }
                else
                {
                    var segment = _segments[(_completed + _offset) / segmentLength];
                    _current = segment.AsMemory().Slice(0, Math.Min(segmentLength, _length - _completed));
                    _completed += _current.Length;
                    return true;
                }
            }
        }
    }
}
