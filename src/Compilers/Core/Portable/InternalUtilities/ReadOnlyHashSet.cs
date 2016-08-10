// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal struct ReadOnlyHashSet<T> : IReadOnlySet<T>
    {
        private HashSet<T> _set;

        public static readonly ReadOnlyHashSet<T> Empty = new ReadOnlyHashSet<T>(new HashSet<T>());

        public ReadOnlyHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer = null)
        {
            _set = new HashSet<T>(collection, comparer);
        }

        public int Count => _set.Count;

        public bool Contains(T item) => _set.Contains(item);
    }
}
