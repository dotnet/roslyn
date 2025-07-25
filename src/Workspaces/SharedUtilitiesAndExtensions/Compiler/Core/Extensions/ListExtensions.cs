// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class ListExtensions
{
    extension<T>(List<T> list) where T : class
    {
        /// <summary>
        /// Update a list in place, where a function has the ability to either transform or remove each item.
        /// </summary>
        /// <typeparam name="T">The type of items in the list.</typeparam>
        /// <typeparam name="TArg">The type of state argument passed to the transformation callback.</typeparam>
        /// <param name="list">The list to update.</param>
        /// <param name="transform">A function which transforms each element. The function returns the transformed list
        /// element, or <see langword="null"/> to remove the current item from the list.</param>
        /// <param name="arg">The state argument to pass to the transformation callback.</param>
        public void RemoveOrTransformAll<TArg>(Func<T, TArg, T?> transform, TArg arg)
        {
            RoslynDebug.AssertNotNull(list);
            RoslynDebug.AssertNotNull(transform);

            var targetIndex = 0;
            for (var sourceIndex = 0; sourceIndex < list.Count; sourceIndex++)
            {
                var newValue = transform(list[sourceIndex], arg);
                if (newValue is null)
                    continue;

                list[targetIndex++] = newValue;
            }

            list.RemoveRange(targetIndex, list.Count - targetIndex);
        }
    }

    extension<T>(List<T> list) where T : struct
    {
        public void RemoveOrTransformAll<TArg>(Func<T, TArg, T?> transform, TArg arg)
        {
            RoslynDebug.AssertNotNull(list);
            RoslynDebug.AssertNotNull(transform);

            var targetIndex = 0;
            for (var sourceIndex = 0; sourceIndex < list.Count; sourceIndex++)
            {
                var newValue = transform(list[sourceIndex], arg);
                if (newValue is null)
                    continue;

                list[targetIndex++] = newValue.Value;
            }

            list.RemoveRange(targetIndex, list.Count - targetIndex);
        }
    }

    extension<T>(ArrayBuilder<T> list) where T : struct
    {
        public void RemoveOrTransformAll<TArg>(Func<T, TArg, T?> transform, TArg arg)
        {
            RoslynDebug.AssertNotNull(list);
            RoslynDebug.AssertNotNull(transform);

            var targetIndex = 0;
            for (var sourceIndex = 0; sourceIndex < list.Count; sourceIndex++)
            {
                var newValue = transform(list[sourceIndex], arg);
                if (newValue is null)
                    continue;

                list[targetIndex++] = newValue.Value;
            }

            list.RemoveRange(targetIndex, list.Count - targetIndex);
        }
    }

    extension<T>(IList<T> list) where T : notnull
    {
        /// <summary>
        /// Attempts to remove the first item selected by <paramref name="selector"/>.
        /// </summary>
        /// <returns>
        /// True if any item has been removed.
        /// </returns>
        public bool TryRemoveFirst<TArg>(Func<T, TArg, bool> selector, TArg arg, [NotNullWhen(true)] out T? removedItem)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (selector(item, arg))
                {
                    list.RemoveAt(i);
                    removedItem = item;
                    return true;
                }
            }

            removedItem = default;
            return false;
        }
    }

    extension<T>(IList<T> list)
    {
        public int IndexOf(Func<T, bool> predicate)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                    return i;
            }

            return -1;
        }

        public int IndexOf<TArg>(Func<T, TArg, bool> predicate, TArg arg)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (predicate(list[i], arg))
                    return i;
            }

            return -1;
        }
    }
}
