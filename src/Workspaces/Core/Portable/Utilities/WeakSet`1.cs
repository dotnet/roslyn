// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A simple collection of values held as weak references. Objects in the set are compared by reference equality.
    /// </summary>
    /// <typeparam name="T">The type of object stored in the set.</typeparam>
    internal sealed class WeakSet<T>
        where T : class?
    {
        private readonly HashSet<ReferenceHolder<T>> _values = new();

        public WeakSet()
        {
        }

        public bool Add(T value)
        {
            if (Contains(value))
                return false;

            return _values.Add(ReferenceHolder<T>.Weak(value));
        }

        public bool Contains(T value)
        {
            return _values.Contains(ReferenceHolder<T>.Strong(value));
        }
    }
}
