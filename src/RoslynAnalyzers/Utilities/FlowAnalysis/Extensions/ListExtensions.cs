// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Analyzer.Utilities.Extensions
{
    internal static class ListExtensions
    {
        /// <summary>
        /// Extract and remove all elements from <paramref name="list"/> which are matched by
        /// <paramref name="predicate"/>.
        /// </summary>
        /// <typeparam name="T">The type of element in the list.</typeparam>
        /// <typeparam name="TArg">A state argument to pass to <paramref name="predicate"/>.</typeparam>
        /// <param name="list">The list.</param>
        /// <param name="predicate">A predicate matching elements to remove from <paramref name="list"/>.</param>
        /// <param name="argument">An additional state argument to pass to <paramref name="predicate"/>.</param>
        /// <returns>A collection of elements removed from <paramref name="list"/>, in the order they were removed. If
        /// no elements were removed, this method returns <see cref="ImmutableArray{T}.Empty"/>.</returns>
        public static ImmutableArray<T> ExtractAll<T, TArg>(this List<T> list, Func<T, TArg, bool> predicate, TArg argument)
        {
            ImmutableArray<T>.Builder? builder = null;
            for (int i = 0; i < list.Count; i++)
            {
                var value = list[i];
                if (predicate(value, argument))
                {
                    builder ??= ImmutableArray.CreateBuilder<T>();
                    builder.Add(value);
                }
                else if (builder is not null)
                {
                    list[i - builder.Count] = value;
                }
            }

            if (builder is null)
            {
                return ImmutableArray<T>.Empty;
            }

            list.RemoveRange(list.Count - builder.Count, builder.Count);
            return builder.Capacity == builder.Count ? builder.MoveToImmutable() : builder.ToImmutable();
        }
    }
}
