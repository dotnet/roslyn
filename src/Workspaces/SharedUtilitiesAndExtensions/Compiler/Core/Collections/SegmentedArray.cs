// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    internal static class SegmentedArray
    {
        public static void CopyTo<T>(this ICollection<T> collection, SegmentedArray<T> array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        internal static void Copy<T>(SegmentedArray<T> sourceArray, SegmentedArray<T> destinationArray, int length)
        {
            Copy(sourceArray, 0, destinationArray, 0, length);
        }

        internal static void Copy<T>(SegmentedArray<T> items, int v, Array array, int arrayIndex, int size)
        {
            throw new NotImplementedException();
        }

        internal static void Copy<T>(SegmentedArray<T> sourceArray, int sourceIndex, SegmentedArray<T> destinationArray, int destinationIndex, int length)
        {
            if (sourceArray.IsDefault)
            {
                throw new ArgumentNullException(nameof(sourceArray));
            }

            if (destinationArray.IsDefault)
            {
                throw new ArgumentNullException(nameof(destinationArray));
            }

            if (sourceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (destinationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationIndex), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (sourceArray.Length - sourceIndex < length
                || destinationArray.Length - destinationIndex < length)
            {
                throw new ArgumentException(CompilerExtensionsResources.Argument_InvalidOffLen);
            }

            if (length == 0)
            {
                return;
            }

            var sourceSpans = new SegmentEnumerable<T>(sourceArray, sourceIndex, length).GetEnumerator();
            var destinationSpans = new SegmentEnumerable<T>(destinationArray, destinationIndex, length).GetEnumerator();

            var sourceWindowIndex = 0;
            sourceSpans.MoveNext();
            var sourceWindow = sourceSpans.Current;

            var destinationWindowIndex = 0;
            destinationSpans.MoveNext();
            var destinationWindow = destinationSpans.Current;

            while (true)
            {
                if (sourceWindowIndex == sourceWindow.Length)
                {
                    if (!sourceSpans.MoveNext())
                    {
                        Debug.Assert(destinationWindowIndex == destinationWindow.Length);
                        Debug.Assert(!destinationSpans.MoveNext());
                        break;
                    }

                    sourceWindow = sourceSpans.Current;
                    sourceWindowIndex = 0;
                }

                if (destinationWindowIndex == destinationWindow.Length)
                {
                    if (!destinationSpans.MoveNext())
                        throw ExceptionUtilities.Unreachable;

                    destinationWindow = destinationSpans.Current;
                    destinationWindowIndex = 0;
                }

                var sourceWindowRemaining = sourceWindow.Length - sourceWindowIndex;
                var destinationWindowRemaining = destinationWindow.Length - destinationWindowIndex;
                var currentCopy = Math.Min(sourceWindowRemaining, destinationWindowRemaining);
                sourceWindow.Slice(sourceWindowIndex, currentCopy).CopyTo(destinationWindow.Slice(destinationWindowIndex, currentCopy));

                sourceWindowIndex += currentCopy;
                destinationWindowIndex += currentCopy;
            }
        }

        internal static void Copy<T>(SegmentedArray<T> items, T[] array, int size)
        {
            throw new NotImplementedException();
        }

        public static void Resize<T>(ref SegmentedArray<T> array, int length)
        {
            throw new NotImplementedException();
        }

        internal static int BinarySearch<T>(SegmentedArray<T> array, int index, int length, T value, IComparer<T> comparer)
        {
            if (array.IsDefault)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (array.Length - index < length)
            {
                throw new ArgumentException(CompilerExtensionsResources.Argument_InvalidOffLen);
            }

            comparer ??= Comparer<T>.Default;

            var lowSegment = index >> SegmentedArray<T>.SegmentShift;
            var highSegment = (index + length - 1) >> SegmentedArray<T>.SegmentShift;
            while (lowSegment < highSegment)
            {
                // Avoid choosing segment == lowSegment because we won't have enough information to make progress
                var segment = lowSegment + ((highSegment - lowSegment + 1) >> 1);
                Debug.Assert(segment > lowSegment, $"Assertion failed: {nameof(segment)} > {nameof(lowSegment)}");

                // Since we haven't chosen the first segment, and all other segments within the search window include
                // index 0 of the segment, we can choose the element at this index for comparison.
                var order = comparer.Compare(array.Segments[segment][0], value);

                if (order == 0)
                {
                    return segment * SegmentedArray<T>.SegmentSize;
                }

                if (order < 0)
                {
                    lowSegment = segment;
                }
                else
                {
                    highSegment = segment - 1;
                }
            }

            // We now search within lowSegment using Array.BinarySearch
            var firstIndexOfLowSegment = lowSegment * SegmentedArray<T>.SegmentSize;
            var lastIndexOfLowSegmentExclusive = firstIndexOfLowSegment + array.Segments[lowSegment].Length;
            var firstIndexToSearch = Math.Max(index, firstIndexOfLowSegment);
            var lastIndexToSearchExclusive = Math.Min(index + length, lastIndexOfLowSegmentExclusive);
            var result = Array.BinarySearch(array.Segments[lowSegment], firstIndexToSearch - firstIndexOfLowSegment, lastIndexToSearchExclusive - firstIndexToSearch, value, comparer);
            if (result >= 0)
                return result + firstIndexOfLowSegment;
            else
                return ~(~result + firstIndexOfLowSegment);
        }

        internal static void Clear<T>(SegmentedArray<T> array, int index, int length)
        {
            foreach (var span in new SegmentEnumerable<T>(array, index, length))
            {
                span.Clear();
            }
        }

        internal static int IndexOf<T>(SegmentedArray<T> array, T value, int startIndex, int count)
        {
            if (array.IsDefault)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Segments.Length == 1)
            {
                return Array.IndexOf(array.Segments[0], value, startIndex, count);
            }

            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (array.Length - startIndex < count)
            {
                throw new ArgumentException(CompilerExtensionsResources.Argument_InvalidOffLen);
            }

            int offset = startIndex;
            foreach (var span in new SegmentEnumerable<T>(array, startIndex, count))
            {
            }

            throw new NotImplementedException();
        }

        internal static int LastIndexOf<T>(SegmentedArray<T> array, T value, int startIndex, int count)
        {
            if (array.IsDefault)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Length == 0)
            {
                return -1;
            }

            if (startIndex < 0 || startIndex >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), CompilerExtensionsResources.ArgumentOutOfRange_Index);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), CompilerExtensionsResources.ArgumentOutOfRange_Count);
            }

            if (count > startIndex + 1)
            {
                throw new ArgumentOutOfRangeException("endIndex", CompilerExtensionsResources.ArgumentOutOfRange_EndIndexStartIndex);
            }

            throw new NotImplementedException();
        }

        internal static void Reverse<T>(SegmentedArray<T> array, int index, int length)
        {
            if (array.IsDefault)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Segments.Length == 1)
            {
                Array.Reverse(array.Segments[0], index, length);
                return;
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (array.Length - index < length)
            {
                throw new ArgumentException(CompilerExtensionsResources.Argument_InvalidOffLen);
            }

            if (length <= 1)
            {
                // No work to do
                return;
            }

            var first = index;
            var last = index + length - 1;
            do
            {
                ref var firstRef = ref array[first];
                ref var lastRef = ref array[last];

                var temp = firstRef;
                firstRef = lastRef;
                lastRef = temp;
                first++;
                last--;
            }
            while (first < last);
        }

        internal static void Sort<T>(SegmentedArray<T> array, int index, int length, IComparer<T> comparer)
        {
            throw new NotImplementedException();
        }

        internal static void Sort<T>(SegmentedArray<T> array, Comparison<T> comparison)
        {
            throw new NotImplementedException();
        }

        private readonly struct SegmentEnumerable<T>
        {
            private readonly SegmentedArray<T> _array;
            private readonly int _index;
            private readonly int _length;

            public SegmentEnumerable(SegmentedArray<T> array, int index, int length)
            {
                if (array.IsDefault)
                {
                    throw new ArgumentNullException(nameof(array));
                }

                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
                }

                if (length < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(length), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
                }

                if (array.Length - index < length)
                {
                    throw new ArgumentException(CompilerExtensionsResources.Argument_InvalidOffLen);
                }

                _array = array;
                _index = index;
                _length = length;
            }

            public Enumerator GetEnumerator()
                => new Enumerator(_array, _index, _length);

            internal struct Enumerator
            {
                private readonly SegmentedArray<T> _array;
                private readonly int _index;
                private readonly int _length;

                private int _nextStartIndex;
                private Memory<T> _current;

                public Enumerator(SegmentedArray<T> array, int index, int length)
                {
                    Debug.Assert(!array.IsDefault);
                    Debug.Assert(!(index < 0));
                    Debug.Assert(!(length < 0));
                    Debug.Assert(!(array.Length - index < length));

                    _array = array;
                    _index = index;
                    _length = length;

                    _nextStartIndex = _index;
                    _current = default;
                }

                public Span<T> Current => _current.Span;

                public bool MoveNext()
                {
                    if (_array.Segments.Length == 1)
                    {
                        if (_nextStartIndex == _index && _length > 0)
                        {
                            // The segmented array only has one segment, so we know the current segment will fit within the page
                            Debug.Assert(!(_array.Segments[0].Length - _index < _length), $"Assertion failed: !({_array.Segments[0].Length} - {_index} < {_length})");
                            _current = _array.Segments[0].AsMemory().Slice(_index, _length);
                            _nextStartIndex += _length;
                            return true;
                        }
                        else
                        {
                            Debug.Assert(_nextStartIndex == _index + _length);
                            return false;
                        }
                    }

                    var startIndex = _nextStartIndex;
                    var totalRemaining = _index + _length - _nextStartIndex;
                    if (totalRemaining == 0)
                    {
                        return false;
                    }

                    var page = _array.Segments[startIndex >> SegmentedArray<T>.SegmentShift];
                    var startIndexInPage = startIndex & SegmentedArray<T>.OffsetMask;
                    var remainingInPage = page.Length - startIndexInPage;
                    var segmentLength = Math.Min(totalRemaining, remainingInPage);
                    _current = page.AsMemory().Slice(startIndexInPage, segmentLength);
                    _nextStartIndex += segmentLength;
                    return true;
                }
            }
        }
    }
}
