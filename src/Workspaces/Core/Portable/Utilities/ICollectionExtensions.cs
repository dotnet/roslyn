// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal static class ICollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T>? values)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (values != null)
            {
                foreach (var item in values)
                {
                    collection.Add(item);
                }
            }
        }

        public static void AddRange<T>(this ICollection<T> collection, ImmutableArray<T> values)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            foreach (var item in values)
            {
                collection.Add(item);
            }
        }
    }
}
