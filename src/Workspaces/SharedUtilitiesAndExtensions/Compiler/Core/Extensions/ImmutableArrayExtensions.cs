// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Collections.Internal;

namespace Roslyn.Utilities
{
    internal static partial class ImmutableArrayExtensions
    {
        public static int FindIndex<T>(this ImmutableArray<T> items, Predicate<T> match)
            => FindIndex(items, 0, items.Length, match);

        public static int FindIndex<T>(this ImmutableArray<T> items, int startIndex, Predicate<T> match)
            => FindIndex(items, startIndex, items.Length - startIndex, match);

        /// <seealso cref="SegmentedList{T}.FindIndex(int, int, Predicate{T})"/>
        public static int FindIndex<T>(this ImmutableArray<T> items, int startIndex, int count, Predicate<T> match)
        {
            if ((uint)startIndex > (uint)items.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual();
            }

            if (count < 0 || startIndex > items.Length - count)
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            }

            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            var endIndex = startIndex + count;
            for (var i = startIndex; i < endIndex; i++)
            {
                if (match(items[i]))
                    return i;
            }

            return -1;
        }

        public static int FindIndex<T, TArg>(this ImmutableArray<T> items, Func<T, TArg, bool> match, TArg arg)
            => FindIndex(items, 0, items.Length, match, arg);

        public static int FindIndex<T, TArg>(this ImmutableArray<T> items, int startIndex, Func<T, TArg, bool> match, TArg arg)
            => FindIndex(items, startIndex, items.Length - startIndex, match, arg);

        /// <seealso cref="SegmentedList{T}.FindIndex(int, int, Predicate{T})"/>
        public static int FindIndex<T, TArg>(this ImmutableArray<T> items, int startIndex, int count, Func<T, TArg, bool> match, TArg arg)
        {
            if ((uint)startIndex > (uint)items.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual();
            }

            if (count < 0 || startIndex > items.Length - count)
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            }

            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            var endIndex = startIndex + count;
            for (var i = startIndex; i < endIndex; i++)
            {
                if (match(items[i], arg))
                    return i;
            }

            return -1;
        }

        public static int FindLastIndex<T>(this ImmutableArray<T> items, Predicate<T> match)
            => FindLastIndex(items, items.Length - 1, items.Length, match);

        public static int FindLastIndex<T>(this ImmutableArray<T> items, int startIndex, Predicate<T> match)
            => FindLastIndex(items, startIndex, startIndex + 1, match);

        /// <seealso cref="SegmentedList{T}.FindLastIndex(int, int, Predicate{T})"/>
        public static int FindLastIndex<T>(this ImmutableArray<T> items, int startIndex, int count, Predicate<T> match)
        {
            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            if (items.Length == 0)
            {
                // Special case for 0 length ImmutableArray
                if (startIndex != -1)
                {
                    ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
                }
            }
            else
            {
                // Make sure we're not out of range
                if ((uint)startIndex >= (uint)items.Length)
                {
                    ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
                }
            }

            // 2nd half of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            }

            var endIndex = startIndex - count;
            for (var i = startIndex; i > endIndex; i--)
            {
                if (match(items[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int FindLastIndex<T, TArg>(this ImmutableArray<T> items, Func<T, TArg, bool> match, TArg arg)
            => FindLastIndex(items, items.Length - 1, items.Length, match, arg);

        public static int FindLastIndex<T, TArg>(this ImmutableArray<T> items, int startIndex, Func<T, TArg, bool> match, TArg arg)
            => FindLastIndex(items, startIndex, startIndex + 1, match, arg);

        /// <seealso cref="SegmentedList{T}.FindLastIndex(int, int, Predicate{T})"/>
        public static int FindLastIndex<T, TArg>(this ImmutableArray<T> items, int startIndex, int count, Func<T, TArg, bool> match, TArg arg)
        {
            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            if (items.Length == 0)
            {
                // Special case for 0 length ImmutableArray
                if (startIndex != -1)
                {
                    ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
                }
            }
            else
            {
                // Make sure we're not out of range
                if ((uint)startIndex >= (uint)items.Length)
                {
                    ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
                }
            }

            // 2nd half of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            }

            var endIndex = startIndex - count;
            for (var i = startIndex; i > endIndex; i--)
            {
                if (match(items[i], arg))
                {
                    return i;
                }
            }

            return -1;
        }

        public static Range FindRange<T>(this ImmutableArray<T> items, Predicate<T> match)
        {
            var firstIndex = FindIndex(items, match);
            if (firstIndex < 0)
                return default;

            var lastIndex = FindLastIndex(items, match);
            Contract.ThrowIfFalse(lastIndex >= firstIndex);
            return new Range(firstIndex, lastIndex + 1);
        }

        public static Range FindRange<T, TArg>(this ImmutableArray<T> items, Func<T, TArg, bool> match, TArg arg)
        {
            var firstIndex = FindIndex(items, match, arg);
            if (firstIndex < 0)
                return default;

            var lastIndex = FindLastIndex(items, match, arg);
            Contract.ThrowIfFalse(lastIndex >= firstIndex);
            return new Range(firstIndex, lastIndex + 1);
        }

        public static ImmutableArray<T> ToImmutableArray<T>(this HashSet<T> set)
        {
            // [.. set] currently allocates, even for the empty case.  Workaround that until that is solved by the compiler.
            if (set.Count == 0)
                return [];

            return [.. set];
        }

        public static bool Contains<T>(this ImmutableArray<T> items, T item, IEqualityComparer<T>? equalityComparer)
            => items.IndexOf(item, 0, equalityComparer) >= 0;

        public static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this T[]? items)
        {
            if (items == null)
            {
                return [];
            }

            return ImmutableArray.Create<T>(items);
        }

        public static ImmutableArray<T> ToImmutableAndClear<T>(this ImmutableArray<T>.Builder builder)
        {
            if (builder.Count == 0)
                return [];

            if (builder.Count == builder.Capacity)
                return builder.MoveToImmutable();

            var result = builder.ToImmutable();
            builder.Clear();
            return result;
        }
    }
}
