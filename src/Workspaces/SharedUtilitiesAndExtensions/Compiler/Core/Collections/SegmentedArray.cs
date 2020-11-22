// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections
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

            if ((uint)length <= (uint)sourceArray.Length
                && (uint)length <= (uint)destinationArray.Length)
            {
                var sourceSegmentEnumerable = sourceArray.GetSegments(0, length);
                var destinationSegmentEnumerable = destinationArray.GetSegments(0, length);

                using var sourceSegmentEnumerator = sourceSegmentEnumerable.GetEnumerator();
                using var destinationSegmentEnumerator = destinationSegmentEnumerable.GetEnumerator();
                while (sourceSegmentEnumerator.MoveNext())
                {
                    destinationSegmentEnumerator.MoveNext();
                    sourceSegmentEnumerator.Current.CopyTo(destinationSegmentEnumerator.Current);
                }

                return;
            }

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (length > sourceArray.Length)
                throw new ArgumentException(SR.Arg_LongerThanSrcArray, nameof(sourceArray));
            if (length > destinationArray.Length)
                throw new ArgumentException(SR.Arg_LongerThanDestArray, nameof(destinationArray));

            throw ExceptionUtilities.Unreachable;
        }

        private static SegmentEnumerable<T> GetSegments<T>(this SegmentedArray<T> array, int offset, int length)
            => new(array, offset, length);

        private readonly struct SegmentEnumerable<T> : IEnumerable<Memory<T>>
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

            IEnumerator<Memory<T>> IEnumerable<Memory<T>>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();
        }

        private struct SegmentEnumerator<T> : IEnumerator<Memory<T>>
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

            object IEnumerator.Current => _current;

            public void Dispose()
            {
            }

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

            public void Reset()
            {
                _completed = 0;
                _current = Memory<T>.Empty;
            }
        }
    }
}
