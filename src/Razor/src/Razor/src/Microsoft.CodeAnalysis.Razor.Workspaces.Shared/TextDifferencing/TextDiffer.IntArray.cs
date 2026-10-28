// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Razor.TextDifferencing;

internal abstract partial class TextDiffer
{
    /// <summary>
    ///  This is a simple wrapper for either a single small int array, or
    ///  an array of int array pages.
    /// </summary>
    private ref struct IntArray
    {
        private readonly struct Page
        {
            public readonly int[] Array;
            public readonly int Start;
            public readonly int Length;

            public Page(int[] array, int start, int length)
                => (Array, Start, Length) = (array, start, length);

            public void Deconstruct(out int[] array, out int start, out int length)
                => (array, start, length) = (Array, Start, Length);
        }

        private const int PageSize = 1024 * 64 / sizeof(int);

        private Page _page;
        private readonly Page[] _pages;

        public int Length { get; }

        public IntArray(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            Length = length;

            var fullSizePageCount = length / PageSize;
            if (fullSizePageCount == 0)
            {
                _pages = Array.Empty<Page>();
                _page = new(RentArray(length), 0, length);
            }
            else
            {
                var finalPageSize = length % PageSize;
                var arraySize = fullSizePageCount;

                // If length is not evenly divisible by PageSize,
                // we must increase the number of pages.
                if (finalPageSize > 0)
                {
                    arraySize++;
                }

                _pages = new Page[arraySize];

                // Rent arrays for the pages that are of length, PageSize.
                for (var i = 0; i < fullSizePageCount; i++)
                {
                    _pages[i] = new(RentArray(PageSize), i * PageSize, PageSize);
                }

                if (finalPageSize > 0)
                {
                    // Rent an array for the final page's length.
                    _pages[^1] = new(RentArray(finalPageSize), fullSizePageCount * PageSize, finalPageSize);
                }

                _page = _pages[0];
            }
        }

        public void Dispose()
        {
            if (_pages.Length == 0)
            {
                // Single page case.
                ReturnArray(_page.Array, clearArray: true);
            }
            else
            {
                foreach (var page in _pages)
                {
                    ReturnArray(page.Array, clearArray: true);
                }
            }
        }

        /// <summary>
        ///  Rents an int array of at least <paramref name="minimumLength"/> from the shared array pool.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int[] RentArray(int minimumLength)
            => ArrayPool<int>.Shared.Rent(minimumLength);

        /// <summary>
        ///  Returns an int array to the shared array pool.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnArray(int[] array, bool clearArray = false)
            => ArrayPool<int>.Shared.Return(array, clearArray);

        public ref int this[int index]
        {
            get
            {
                var page = _page;

                // Does this index fall within page? If not, acquire the appropriate page.
                if (index < page.Start || index >= page.Start + page.Length)
                {
                    page = _pages[index / PageSize];
                    _page = page;
                }

                return ref page.Array[index - page.Start];
            }
        }
    }
}
