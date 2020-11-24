// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
                foreach (var (first, second) in GetSegments(sourceArray, destinationArray, length))
                {
                    first.CopyTo(second);
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

        public static void Copy<T>(SegmentedArray<T> sourceArray, Array destinationArray, int length)
        {
            if (destinationArray is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.destinationArray);
            if (length == 0)
                return;

            if ((uint)length <= (uint)sourceArray.Length
                && (uint)length <= (uint)destinationArray.Length)
            {
                var copied = 0;
                foreach (var memory in sourceArray.GetSegments(0, length))
                {
                    if (!MemoryMarshal.TryGetArray<T>(memory, out var segment))
                    {
                        throw new NotSupportedException();
                    }

                    Array.Copy(segment.Array!, segment.Offset, destinationArray, copied, segment.Count);
                    copied += segment.Count;
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

        public static void Copy<T>(SegmentedArray<T> sourceArray, int sourceIndex, SegmentedArray<T> destinationArray, int destinationIndex, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), SR.ArgumentOutOfRange_ArrayLB);
            if (destinationIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex), SR.ArgumentOutOfRange_ArrayLB);
            if ((uint)(sourceIndex + length) > sourceArray.Length)
                throw new ArgumentException(SR.Arg_LongerThanSrcArray, nameof(sourceArray));
            if ((uint)(destinationIndex + length) > destinationArray.Length)
                throw new ArgumentException(SR.Arg_LongerThanDestArray, nameof(destinationArray));

            if (length == 0)
                return;

            foreach (var (first, second) in GetSegmentsUnaligned(sourceArray, sourceIndex, destinationArray, destinationIndex, length))
            {
                first.CopyTo(second);
            }
        }

        public static void Copy<T>(SegmentedArray<T> sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
        {
            if (destinationArray == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.destinationArray);

            if (typeof(T[]) != destinationArray.GetType() && destinationArray.Rank != 1)
                throw new RankException(SR.Rank_MustMatch);

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), SR.ArgumentOutOfRange_ArrayLB);

            var dstLB = destinationArray.GetLowerBound(0);
            if (destinationIndex < dstLB || destinationIndex - dstLB < 0)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex), SR.ArgumentOutOfRange_ArrayLB);
            destinationIndex -= dstLB;

            if ((uint)(sourceIndex + length) > sourceArray.Length)
                throw new ArgumentException(SR.Arg_LongerThanSrcArray, nameof(sourceArray));
            if ((uint)(destinationIndex + length) > (nuint)destinationArray.LongLength)
                throw new ArgumentException(SR.Arg_LongerThanDestArray, nameof(destinationArray));

            var copied = 0;
            foreach (var memory in sourceArray.GetSegments(0, length))
            {
                if (!MemoryMarshal.TryGetArray<T>(memory, out var segment))
                {
                    throw new NotSupportedException();
                }

                Array.Copy(segment.Array!, segment.Offset, destinationArray, copied, segment.Count);
                copied += segment.Count;
            }
        }

        public static int BinarySearch<T>(SegmentedArray<T> array, T value)
        {
            return BinarySearch(array, 0, array.Length, value, comparer: null);
        }

        public static int BinarySearch<T>(SegmentedArray<T> array, T value, IComparer<T>? comparer)
        {
            return BinarySearch(array, 0, array.Length, value, comparer);
        }

        public static int BinarySearch<T>(SegmentedArray<T> array, int index, int length, T value)
        {
            return BinarySearch(array, index, length, value, comparer: null);
        }

        public static int BinarySearch<T>(SegmentedArray<T> array, int index, int length, T value, IComparer<T>? comparer)
        {
            if (index < 0)
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
            if (array.Length - index < length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);

            try
            {
                comparer ??= Comparer<T>.Default;
                return InternalBinarySearch(array, index, length, value, comparer);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
                return 0;
            }

            static int InternalBinarySearch(SegmentedArray<T> array, int index, int length, T value, IComparer<T> comparer)
            {
                Debug.Assert(index >= 0 && length >= 0 && (array.Length - index >= length), "Check the arguments in the caller!");

                var lo = index;
                var hi = index + length - 1;
                while (lo <= hi)
                {
                    var i = lo + ((hi - lo) >> 1);
                    var order = comparer.Compare(array[i], value);

                    if (order == 0)
                        return i;

                    if (order < 0)
                    {
                        lo = i + 1;
                    }
                    else
                    {
                        hi = i - 1;
                    }
                }

                return ~lo;
            }
        }

        public static int IndexOf<T>(SegmentedArray<T> array, T value)
        {
            return IndexOf(array, value, 0, array.Length);
        }

        public static int IndexOf<T>(SegmentedArray<T> array, T value, int startIndex)
        {
            return IndexOf(array, value, startIndex, array.Length - startIndex);
        }

        public static int IndexOf<T>(SegmentedArray<T> array, T value, int startIndex, int count)
        {
            if ((uint)startIndex > (uint)array.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index();
            }

            if ((uint)count > (uint)(array.Length - startIndex))
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            }

            var offset = startIndex;
            foreach (var memory in array.GetSegments(startIndex, count))
            {
                if (!MemoryMarshal.TryGetArray<T>(memory, out var segment))
                {
                    throw new NotSupportedException();
                }

                var index = Array.IndexOf(segment.Array!, value, segment.Offset, segment.Count);
                if (index > 0)
                {
                    return index + offset;
                }

                offset += segment.Count;
            }

            return -1;
        }

        public static int LastIndexOf<T>(SegmentedArray<T> array, T value)
        {
            return LastIndexOf(array, value, array.Length - 1, array.Length);
        }

        public static int LastIndexOf<T>(SegmentedArray<T> array, T value, int startIndex)
        {
            return LastIndexOf(array, value, startIndex, array.Length == 0 ? 0 : startIndex + 1);
        }

        public static int LastIndexOf<T>(SegmentedArray<T> array, T value, int startIndex, int count)
        {
            if (array.Length == 0)
            {
                // Special case for 0 length List
                // accept -1 and 0 as valid startIndex for compatibility reason.
                if (startIndex != -1 && startIndex != 0)
                {
                    ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index();
                }

                // only 0 is a valid value for count if array is empty
                if (count != 0)
                {
                    ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
                }

                return -1;
            }

            // Make sure we're not out of range
            if ((uint)startIndex >= (uint)array.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index();
            }

            // 2nd half of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            }

            var endIndex = startIndex - count + 1;
            for (var i = startIndex; i >= endIndex; i--)
            {
                if (EqualityComparer<T>.Default.Equals(array[i], value))
                    return i;
            }

            return -1;
        }

        public static void Reverse<T>(SegmentedArray<T> array)
        {
            Reverse(array, 0, array.Length);
        }

        public static void Reverse<T>(SegmentedArray<T> array, int index, int length)
        {
            if (index < 0)
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
            if (array.Length - index < length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);

            if (length <= 1)
                return;

            var firstIndex = index;
            var lastIndex = index + length - 1;
            do
            {
                var temp = array[firstIndex];
                array[firstIndex] = array[lastIndex];
                array[lastIndex] = temp;
                firstIndex++;
                lastIndex--;
            } while (firstIndex < lastIndex);
        }

        public static void Sort<T>(SegmentedArray<T> array)
        {
            Sort(array, 0, array.Length, comparer: null);
        }

        public static void Sort<T>(SegmentedArray<T> array, int index, int length)
        {
            Sort(array, index, length, comparer: null);
        }

        public static void Sort<T>(SegmentedArray<T> array, IComparer<T>? comparer)
        {
            Sort(array, 0, array.Length, comparer);
        }

        public static void Sort<T>(SegmentedArray<T> array, int index, int length, IComparer<T>? comparer)
        {
            if (index < 0)
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
            if (array.Length - index < length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);

            comparer ??= Comparer<T>.Default;

            var current = index;
            foreach (var value in array.Skip(index).Take(length).OrderBy(comparer).ToArray())
            {
                array[current++] = value;
            }
        }

        public static void Sort<T>(SegmentedArray<T> array, Comparison<T> comparison)
        {
            Sort(array, 0, array.Length, Comparer<T>.Create(comparison));
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

        private readonly struct AlignedSegmentEnumerable<T> : IEnumerable<(Memory<T> first, Memory<T> second)>
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

            IEnumerator<(Memory<T> first, Memory<T> second)> IEnumerable<(Memory<T> first, Memory<T> second)>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();
        }

        private struct AlignedSegmentEnumerator<T> : IEnumerator<(Memory<T> first, Memory<T> second)>
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

            object IEnumerator.Current => _current;

            public void Dispose()
            {
            }

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

            public void Reset()
            {
                _completed = 0;
                _current = (Memory<T>.Empty, Memory<T>.Empty);
            }
        }

        private readonly struct UnalignedSegmentEnumerable<T> : IEnumerable<(Memory<T> first, Memory<T> second)>
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

            IEnumerator<(Memory<T> first, Memory<T> second)> IEnumerable<(Memory<T> first, Memory<T> second)>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();
        }

        private struct UnalignedSegmentEnumerator<T> : IEnumerator<(Memory<T> first, Memory<T> second)>
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

            object IEnumerator.Current => _current;

            public void Dispose()
            {
            }

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

            public void Reset()
            {
                _completed = 0;
                _current = (Memory<T>.Empty, Memory<T>.Empty);
            }
        }

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
                => new((T[][])_array.SyncRoot, _offset, _length);

            IEnumerator<Memory<T>> IEnumerable<Memory<T>>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            public ReverseEnumerable Reverse()
                => new(this);

            public readonly struct ReverseEnumerable : IEnumerable<Memory<T>>
            {
                private readonly SegmentEnumerable<T> _enumerable;

                public ReverseEnumerable(SegmentEnumerable<T> enumerable)
                {
                    _enumerable = enumerable;
                }

                public SegmentEnumerator<T>.Reverse GetEnumerator()
                    => new((T[][])_enumerable._array.SyncRoot, _enumerable._offset, _enumerable._length);

                IEnumerator<Memory<T>> IEnumerable<Memory<T>>.GetEnumerator()
                    => GetEnumerator();

                IEnumerator IEnumerable.GetEnumerator()
                    => GetEnumerator();

                public SegmentEnumerable<T> Reverse()
                    => _enumerable;
            }
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

            public struct Reverse : IEnumerator<Memory<T>>
            {
                private readonly T[][] _segments;
                private readonly int _offset;
                private readonly int _length;

                private int _completed;
                private Memory<T> _current;

                public Reverse(T[][] segments, int offset, int length)
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
}
