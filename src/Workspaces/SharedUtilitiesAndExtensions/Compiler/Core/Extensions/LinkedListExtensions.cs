// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class LinkedListExtensions
{
    public static void AddRangeAtHead<T>(this LinkedList<T> list, IEnumerable<T> values)
    {
        LinkedListNode<T>? currentNode = null;
        foreach (var value in values)
        {
            if (currentNode == null)
            {
                currentNode = list.AddFirst(value);
            }
            else
            {
                currentNode = list.AddAfter(currentNode, value);
            }
        }
    }
}
