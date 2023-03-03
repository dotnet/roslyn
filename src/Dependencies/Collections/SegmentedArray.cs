// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        public static void Copy<T>(SegmentedArray<T> sourceArray, Array destinationArray, int length)
        {
            if (destinationArray is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.destinationArray);
            if (length == 0)
                return;

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (length > sourceArray.Length)
                throw new ArgumentException(SR.Arg_LongerThanSrcArray, nameof(sourceArray));
            if (length > destinationArray.Length)
                throw new ArgumentException(SR.Arg_LongerThanDestArray, nameof(destinationArray));

            var copied = 0;
            foreach (var memory in sourceArray.GetSegments(0, length))
            {
                if (!MemoryMarshal.TryGetArray<T>(memory, out var segment))
                {
                    throw new NotSupportedException();
                }

                Array.Copy(segment.Array!, sourceIndex: segment.Offset, destinationArray: destinationArray, destinationIndex: copied, length: segment.Count);
                copied += segment.Count;
            }
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

            if (sourceArray.SyncRoot == destinationArray.SyncRoot
                && sourceIndex + length > destinationIndex)
            {
                // We are copying in the same array with overlap
                CopyOverlapped(sourceArray, sourceIndex, destinationIndex, length);
            }
            else
            {
                foreach (var (first, second) in GetSegmentsUnaligned(sourceArray, sourceIndex, destinationArray, destinationIndex, length))
                {
                    first.CopyTo(second);
                }
            }
        }

        // PERF: Avoid inlining this path in Copy<T>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CopyOverlapped<T>(SegmentedArray<T> array, int sourceIndex, int destinationIndex, int length)
        {
            Debug.Assert(length > 0);
            Debug.Assert(sourceIndex >= 0);
            Debug.Assert(destinationIndex >= 0);
            Debug.Assert((uint)(sourceIndex + length) <= array.Length);
            Debug.Assert((uint)(destinationIndex + length) <= array.Length);

            var unalignedEnumerator = GetSegmentsUnaligned(array, sourceIndex, array, destinationIndex, length);
            if (sourceIndex < destinationIndex)
            {
                // We are copying forward in the same array with overlap
                foreach (var (first, second) in unalignedEnumerator.Reverse())
                {
                    first.CopyTo(second);
                }
            }
            else
            {
                foreach (var (first, second) in unalignedEnumerator)
                {
                    first.CopyTo(second);
                }
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
            foreach (var memory in sourceArray.GetSegments(sourceIndex, length))
            {
                if (!MemoryMarshal.TryGetArray<T>(memory, out var segment))
                {
                    throw new NotSupportedException();
                }

                Array.Copy(segment.Array!, sourceIndex: segment.Offset, destinationArray: destinationArray, destinationIndex: destinationIndex + copied, length: segment.Count);
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

            return SegmentedArraySortHelper<T>.BinarySearch(array, index, length, value, comparer);
        }

        public static int IndexOf<T>(SegmentedArray<T> array, T value)
        {
            return IndexOf(array, value, 0, array.Length, comparer: null);
        }

        public static int IndexOf<T>(SegmentedArray<T> array, T value, int startIndex)
        {
            return IndexOf(array, value, startIndex, array.Length - startIndex, comparer: null);
        }

        public static int IndexOf<T>(SegmentedArray<T> array, T value, int startIndex, int count)
        {
            return IndexOf(array, value, startIndex, count, comparer: null);
        }

        public static int IndexOf<T>(SegmentedArray<T> array, T value, int startIndex, int count, IEqualityComparer<T>? comparer)
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

                int index;
                if (comparer is null || comparer == EqualityComparer<T>.Default)
                {
                    index = Array.IndexOf(segment.Array!, value, segment.Offset, segment.Count);
                }
                else
                {
                    index = -1;
                    var endIndex = segment.Offset + segment.Count;
                    for (var i = segment.Offset; i < endIndex; i++)
                    {
                        if (comparer.Equals(array[i], value))
                        {
                            index = i;
                            break;
                        }
                    }
                }

                if (index >= 0)
                {
                    return index + offset - segment.Offset;
                }

                offset += segment.Count;
            }

            return -1;
        }

        public static int LastIndexOf<T>(SegmentedArray<T> array, T value)
        {
            return LastIndexOf(array, value, array.Length - 1, array.Length, comparer: null);
        }

        public static int LastIndexOf<T>(SegmentedArray<T> array, T value, int startIndex)
        {
            return LastIndexOf(array, value, startIndex, array.Length == 0 ? 0 : startIndex + 1, comparer: null);
        }

        public static int LastIndexOf<T>(SegmentedArray<T> array, T value, int startIndex, int count)
        {
            return LastIndexOf(array, value, startIndex, count, comparer: null);
        }

        public static int LastIndexOf<T>(SegmentedArray<T> array, T value, int startIndex, int count, IEqualityComparer<T>? comparer)
        {
            if (array.Length == 0)
            {
                // Special case for 0 length List
                // accept -1 and 0 as valid startIndex for compatibility reason.
                if (startIndex is not (-1) and not 0)
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

            if (comparer is null || comparer == EqualityComparer<T>.Default)
            {
                var endIndex = startIndex - count + 1;
                for (var i = startIndex; i >= endIndex; i--)
                {
                    if (EqualityComparer<T>.Default.Equals(array[i], value))
                        return i;
                }
            }
            else
            {
                var endIndex = startIndex - count + 1;
                for (var i = startIndex; i >= endIndex; i--)
                {
                    if (comparer.Equals(array[i], value))
                        return i;
                }
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
            if (array.Length > 1)
            {
                var segment = new SegmentedArraySegment<T>(array, 0, array.Length);
                SegmentedArraySortHelper<T>.Sort(segment, (IComparer<T>?)null);
            }
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

            if (length > 1)
            {
                var segment = new SegmentedArraySegment<T>(array, index, length);
                SegmentedArraySortHelper<T>.Sort(segment, comparer);
            }
        }

        public static void Sort<T>(SegmentedArray<T> array, Comparison<T> comparison)
        {
            if (comparison is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparison);
            }

            if (array.Length > 1)
            {
                var segment = new SegmentedArraySegment<T>(array, 0, array.Length);
                SegmentedArraySortHelper<T>.Sort(segment, comparison);
            }
        }

        private static SegmentEnumerable<T> GetSegments<T>(this SegmentedArray<T> array, int offset, int length)
            => new(array, offset, length);

        private static AlignedSegmentEnumerable<T> GetSegments<T>(SegmentedArray<T> first, SegmentedArray<T> second, int length)
            => new(first, second, length);

#pragma warning disable IDE0051 // Remove unused private members (will be used in follow-up)
        private static AlignedSegmentEnumerable<T> GetSegmentsAligned<T>(SegmentedArray<T> first, int firstOffset, SegmentedArray<T> second, int secondOffset, int length)
            => new(first, firstOffset, second, secondOffset, length);
#pragma warning restore IDE0051 // Remove unused private members

        private static UnalignedSegmentEnumerable<T> GetSegmentsUnaligned<T>(SegmentedArray<T> first, int firstOffset, SegmentedArray<T> second, int secondOffset, int length)
            => new(first, firstOffset, second, secondOffset, length);

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

            public readonly (Memory<T> first, Memory<T> second) Current => _current;

            public bool MoveNext()
            {
                if (_completed == _length)
                {
                    _current = (Memory<T>.Empty, Memory<T>.Empty);
                    return false;
                }

                if (_completed == 0)
                {
                    var initialFirstSegment = _firstOffset >> SegmentedArrayHelper.GetSegmentShift<T>();
                    var initialSecondSegment = _secondOffset >> SegmentedArrayHelper.GetSegmentShift<T>();
                    var offset = _firstOffset & SegmentedArrayHelper.GetOffsetMask<T>();
                    Debug.Assert(offset == (_secondOffset & SegmentedArrayHelper.GetOffsetMask<T>()), "Aligned views must start at the same segment offset");

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
                    var firstSegment = _firstSegments[(_completed + _firstOffset) >> SegmentedArrayHelper.GetSegmentShift<T>()];
                    var secondSegment = _secondSegments[(_completed + _secondOffset) >> SegmentedArrayHelper.GetSegmentShift<T>()];
                    var currentSegmentLength = Math.Min(SegmentedArrayHelper.GetSegmentSize<T>(), _length - _completed);
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

            public ReverseEnumerable Reverse()
                => new(this);

            public readonly struct ReverseEnumerable
            {
                private readonly UnalignedSegmentEnumerable<T> _enumerable;

                public ReverseEnumerable(UnalignedSegmentEnumerable<T> enumerable)
                {
                    _enumerable = enumerable;
                }

                public UnalignedSegmentEnumerator<T>.Reverse GetEnumerator()
                => new((T[][])_enumerable._first.SyncRoot, _enumerable._firstOffset, (T[][])_enumerable._second.SyncRoot, _enumerable._secondOffset, _enumerable._length);

                public UnalignedSegmentEnumerable<T> Reverse()
                    => _enumerable;
            }
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

            public readonly (Memory<T> first, Memory<T> second) Current => _current;

            public bool MoveNext()
            {
                if (_completed == _length)
                {
                    _current = (Memory<T>.Empty, Memory<T>.Empty);
                    return false;
                }

                var initialFirstSegment = (_completed + _firstOffset) >> SegmentedArrayHelper.GetSegmentShift<T>();
                var initialSecondSegment = (_completed + _secondOffset) >> SegmentedArrayHelper.GetSegmentShift<T>();
                var firstOffset = (_completed + _firstOffset) & SegmentedArrayHelper.GetOffsetMask<T>();
                var secondOffset = (_completed + _secondOffset) & SegmentedArrayHelper.GetOffsetMask<T>();

                var firstSegment = _firstSegments[initialFirstSegment];
                var secondSegment = _secondSegments[initialSecondSegment];
                var remainingInFirstSegment = firstSegment.Length - firstOffset;
                var remainingInSecondSegment = secondSegment.Length - secondOffset;
                var currentSegmentLength = Math.Min(Math.Min(remainingInFirstSegment, remainingInSecondSegment), _length - _completed);
                _current = (firstSegment.AsMemory().Slice(firstOffset, currentSegmentLength), secondSegment.AsMemory().Slice(secondOffset, currentSegmentLength));
                _completed += currentSegmentLength;
                return true;
            }

            public struct Reverse
            {
                private readonly T[][] _firstSegments;
                private readonly int _firstOffset;
                private readonly T[][] _secondSegments;
                private readonly int _secondOffset;
                private readonly int _length;

                private int _completed;
                private (Memory<T> first, Memory<T> second) _current;

                public Reverse(T[][] firstSegments, int firstOffset, T[][] secondSegments, int secondOffset, int length)
                {
                    _firstSegments = firstSegments;
                    _firstOffset = firstOffset;
                    _secondSegments = secondSegments;
                    _secondOffset = secondOffset;
                    _length = length;

                    _completed = 0;
                    _current = (Memory<T>.Empty, Memory<T>.Empty);
                }

                public readonly (Memory<T> first, Memory<T> second) Current => _current;

                public bool MoveNext()
                {
                    if (_completed == _length)
                    {
                        _current = (Memory<T>.Empty, Memory<T>.Empty);
                        return false;
                    }

                    var initialFirstSegment = (_firstOffset + _length - _completed - 1) >> SegmentedArrayHelper.GetSegmentShift<T>();
                    var initialSecondSegment = (_secondOffset + _length - _completed - 1) >> SegmentedArrayHelper.GetSegmentShift<T>();
                    var firstOffset = (_firstOffset + _length - _completed - 1) & SegmentedArrayHelper.GetOffsetMask<T>();
                    var secondOffset = (_secondOffset + _length - _completed - 1) & SegmentedArrayHelper.GetOffsetMask<T>();

                    var firstSegment = _firstSegments[initialFirstSegment];
                    var secondSegment = _secondSegments[initialSecondSegment];
                    var remainingInFirstSegment = firstOffset + 1;
                    var remainingInSecondSegment = secondOffset + 1;
                    var currentSegmentLength = Math.Min(Math.Min(remainingInFirstSegment, remainingInSecondSegment), _length - _completed);
                    _current = (firstSegment.AsMemory().Slice(firstOffset - currentSegmentLength + 1, currentSegmentLength), secondSegment.AsMemory().Slice(secondOffset - currentSegmentLength + 1, currentSegmentLength));
                    _completed += currentSegmentLength;
                    return true;
                }
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
                => new((T[][])_array.SyncRoot, _offset, _length);

            public ReverseEnumerable Reverse()
                => new(this);

            public readonly struct ReverseEnumerable
            {
                private readonly SegmentEnumerable<T> _enumerable;

                public ReverseEnumerable(SegmentEnumerable<T> enumerable)
                {
                    _enumerable = enumerable;
                }

                public SegmentEnumerator<T>.Reverse GetEnumerator()
                    => new((T[][])_enumerable._array.SyncRoot, _enumerable._offset, _enumerable._length);

                public SegmentEnumerable<T> Reverse()
                    => _enumerable;
            }
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

            public readonly Memory<T> Current => _current;

            public bool MoveNext()
            {
                if (_completed == _length)
                {
                    _current = Memory<T>.Empty;
                    return false;
                }

                if (_completed == 0)
                {
                    var firstSegment = _offset >> SegmentedArrayHelper.GetSegmentShift<T>();
                    var offset = _offset & SegmentedArrayHelper.GetOffsetMask<T>();

                    var segment = _segments[firstSegment];
                    var remainingInSegment = segment.Length - offset;
                    _current = segment.AsMemory().Slice(offset, Math.Min(remainingInSegment, _length));
                    _completed = _current.Length;
                    return true;
                }
                else
                {
                    var segment = _segments[(_completed + _offset) >> SegmentedArrayHelper.GetSegmentShift<T>()];
                    _current = segment.AsMemory().Slice(0, Math.Min(SegmentedArrayHelper.GetSegmentSize<T>(), _length - _completed));
                    _completed += _current.Length;
                    return true;
                }
            }

            public struct Reverse
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

                public readonly Memory<T> Current => _current;

                public bool MoveNext()
                {
                    if (_completed == _length)
                    {
                        _current = Memory<T>.Empty;
                        return false;
                    }

                    if (_completed == 0)
                    {
                        var firstSegment = _offset >> SegmentedArrayHelper.GetSegmentShift<T>();
                        var offset = _offset & SegmentedArrayHelper.GetOffsetMask<T>();

                        var segment = _segments[firstSegment];
                        var remainingInSegment = segment.Length - offset;
                        _current = segment.AsMemory().Slice(offset, Math.Min(remainingInSegment, _length));
                        _completed = _current.Length;
                        return true;
                    }
                    else
                    {
                        var segment = _segments[(_completed + _offset) >> SegmentedArrayHelper.GetSegmentShift<T>()];
                        _current = segment.AsMemory().Slice(0, Math.Min(SegmentedArrayHelper.GetSegmentSize<T>(), _length - _completed));
                        _completed += _current.Length;
                        return true;
                    }
                }
            }
        }
    }
}
