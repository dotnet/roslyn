// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
