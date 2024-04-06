// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Roslyn.Utilities;

internal static class TopologicalSorter
{
    public static IEnumerable<T> TopologicalSort<T>(this IEnumerable<T> items, Func<T, IEnumerable<T>> itemsBefore)
    {
        var result = new List<T>();
        var visited = new HashSet<T>();

        foreach (var item in items)
        {
            Visit(item, itemsBefore, result, visited);
        }

        return result;
    }

    public static IEnumerable<T> TopologicalSort<T>(this IEnumerable<T> items, Func<T, IEnumerable<T>> itemsBefore, Func<T, IEnumerable<T>> itemsAfter)
        where T : notnull
    {
        var combinedItemsBefore = CreateCombinedItemsBefore(items, itemsBefore, itemsAfter);
        return TopologicalSort(items, combinedItemsBefore);
    }

    private static void Visit<T>(
        T item,
        Func<T, IEnumerable<T>> itemsBefore,
        List<T> result,
        HashSet<T> visited)
    {
        if (visited.Add(item))
        {
            foreach (var before in itemsBefore(item))
            {
                Visit(before, itemsBefore, result, visited);
            }

            result.Add(item);
        }
    }

    private static Func<T, IEnumerable<T>> CreateCombinedItemsBefore<T>(IEnumerable<T> items, Func<T, IEnumerable<T>> itemsBefore, Func<T, IEnumerable<T>> itemsAfter)
        where T : notnull
    {
        // create initial list
        var itemToItemsBefore = items.ToDictionary(item => item, item =>
        {
            var naturalItemsBefore = itemsBefore != null ? itemsBefore(item) : null;
            if (naturalItemsBefore != null)
            {
                return naturalItemsBefore.ToList();
            }
            else
            {
                return new List<T>();
            }
        });

        // add items after by making the after items explicitly list the item as before it
        if (itemsAfter != null)
        {
            foreach (var item in items)
            {
                var naturalItemsAfter = itemsAfter(item);
                if (naturalItemsAfter != null)
                {
                    foreach (var itemAfter in naturalItemsAfter)
                    {
                        var itemsAfterBeforeList = itemToItemsBefore[itemAfter];
                        itemsAfterBeforeList.Add(item);
                    }
                }
            }
        }

        return item => itemToItemsBefore[item];
    }
}
