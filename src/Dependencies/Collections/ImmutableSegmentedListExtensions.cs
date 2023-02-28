// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;

#pragma warning disable RS0001 // Use 'SpecializedCollections.EmptyEnumerable()'

namespace System.Linq
{
    /// <seealso cref="ImmutableArrayExtensions"/>
    internal static class ImmutableSegmentedListExtensions
    {
        public static bool All<T>(this ImmutableSegmentedList<T> immutableList, Func<T, bool> predicate)
        {
            if (immutableList.IsDefault)
                throw new ArgumentNullException(nameof(immutableList));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            foreach (var item in immutableList)
            {
                if (!predicate(item))
                    return false;
            }

            return true;
        }

        public static bool Any<T>(this ImmutableSegmentedList<T> immutableList)
        {
            if (immutableList.IsDefault)
                throw new ArgumentNullException(nameof(immutableList));

            return !immutableList.IsEmpty;
        }

        public static bool Any<T>(this ImmutableSegmentedList<T>.Builder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Count > 0;
        }

        public static bool Any<T>(this ImmutableSegmentedList<T> immutableList, Func<T, bool> predicate)
        {
            if (immutableList.IsDefault)
                throw new ArgumentNullException(nameof(immutableList));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            foreach (var item in immutableList)
            {
                if (predicate(item))
                    return true;
            }

            return false;
        }

        public static T Last<T>(this ImmutableSegmentedList<T> immutableList)
        {
            // In the event of an empty list, generate the same exception
            // that the linq extension method would.
            return immutableList.Count > 0
                ? immutableList[immutableList.Count - 1]
                : Enumerable.Last(immutableList);
        }

        public static T Last<T>(this ImmutableSegmentedList<T>.Builder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            // In the event of an empty list, generate the same exception
            // that the linq extension method would.
            return builder.Count > 0
                ? builder[builder.Count - 1]
                : Enumerable.Last(builder);
        }

        public static T Last<T>(this ImmutableSegmentedList<T> immutableList, Func<T, bool> predicate)
        {
            if (immutableList.IsDefault)
                throw new ArgumentNullException(nameof(immutableList));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            for (var i = immutableList.Count - 1; i >= 0; i--)
            {
                if (predicate(immutableList[i]))
                    return immutableList[i];
            }

            // Throw the same exception that LINQ would.
            return Enumerable.Empty<T>().Last();
        }

        public static IEnumerable<TResult> Select<T, TResult>(this ImmutableSegmentedList<T> immutableList, Func<T, TResult> selector)
        {
            if (immutableList.IsDefault)
                throw new ArgumentNullException(nameof(immutableList));
            if (selector is null)
                throw new ArgumentNullException(nameof(selector));

            if (immutableList.IsEmpty)
            {
                return Enumerable.Empty<TResult>();
            }
            else
            {
                return Enumerable.Select(immutableList, selector);
            }
        }

        /// <summary>
        /// Creates a new immutable segmented list based on filtered elements by the predicate. The list must not be null.
        /// </summary>
        /// <param name="list">The list to process</param>
        /// <param name="predicate">The delegate that defines the conditions of the element to search for.</param>
        public static ImmutableSegmentedList<T> WhereAsSegmented<T>(this ImmutableSegmentedList<T> list, Func<T, bool> predicate)
            => WhereAsSegmentedImpl<T, object?>(list, predicate, predicateWithArg: null, arg: null);

        /// <summary>
        /// Creates a new immutable segmented list based on filtered elements by the predicate. The list must not be null.
        /// </summary>
        /// <param name="list">The list to process</param>
        /// <param name="predicate">The delegate that defines the conditions of the element to search for.</param>
        public static ImmutableSegmentedList<T> WhereAsSegmented<T, TArg>(this ImmutableSegmentedList<T> list, Func<T, TArg, bool> predicate, TArg arg)
            => WhereAsSegmentedImpl(list, predicateWithoutArg: null, predicate, arg);

        private static ImmutableSegmentedList<T> WhereAsSegmentedImpl<T, TArg>(ImmutableSegmentedList<T> list, Func<T, bool>? predicateWithoutArg, Func<T, TArg, bool>? predicateWithArg, TArg arg)
        {
            Debug.Assert(!list.IsDefault);
            Debug.Assert(predicateWithArg != null ^ predicateWithoutArg != null);

            ImmutableSegmentedList<T>.Builder? builder = null;
            bool none = true;
            bool all = true;

            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                var a = list[i];

                if ((predicateWithoutArg != null) ? predicateWithoutArg(a) : predicateWithArg!(a, arg))
                {
                    none = false;
                    if (all)
                    {
                        continue;
                    }

                    Debug.Assert(i > 0);
                    if (builder == null)
                    {
                        builder = ImmutableSegmentedList.CreateBuilder<T>();
                    }

                    builder.Add(a);
                }
                else
                {
                    if (none)
                    {
                        all = false;
                        continue;
                    }

                    Debug.Assert(i > 0);
                    if (all)
                    {
                        Debug.Assert(builder == null);
                        all = false;
                        builder = ImmutableSegmentedList.CreateBuilder<T>();
                        for (int j = 0; j < i; j++)
                        {
                            builder.Add(list[j]);
                        }
                    }
                }
            }

            if (builder != null)
            {
                Debug.Assert(!all);
                Debug.Assert(!none);
                return builder.ToImmutable();
            }
            else if (all)
            {
                return list;
            }
            else
            {
                Debug.Assert(none);
                return ImmutableSegmentedList<T>.Empty;
            }
        }
    }
}
