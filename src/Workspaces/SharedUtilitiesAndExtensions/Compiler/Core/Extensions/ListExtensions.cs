// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ListExtensions
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
        public static void RemoveOrTransformAll<T, TArg>(this List<T> list, Func<T, TArg, T?> transform, TArg arg)
            where T : class
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
}
