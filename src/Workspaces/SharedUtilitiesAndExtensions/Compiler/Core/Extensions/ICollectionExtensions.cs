// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ICollectionExtensions
    {
        public static void RemoveRange<T>(this ICollection<T> collection, IEnumerable<T>? items)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (items != null)
            {
                foreach (var item in items)
                {
                    collection.Remove(item);
                }
            }
        }
    }
}
