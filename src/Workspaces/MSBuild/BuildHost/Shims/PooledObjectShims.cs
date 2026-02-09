// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Minimal shims for types referenced by shared source files (PathUtilities.cs,
// CaseInsensitiveComparison.cs) so the BuildHost project can compile without
// the PooledObjects / Collections projitems and System.Collections.Immutable.

using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    /// <summary>
    /// Lightweight stand-in for the real ArrayBuilder used by PathUtilities.ResolveParts.
    /// </summary>
    internal sealed class ArrayBuilder<T>
    {
        private readonly List<T> _items = new();

        public int Count => _items.Count;

        public static ArrayBuilder<T> GetInstance() => new();

        public void Push(T item) => _items.Add(item);

        public void Pop()
        {
            if (_items.Count > 0)
                _items.RemoveAt(_items.Count - 1);
        }

        public void Free() { }

        public void ReverseContents() => _items.Reverse();

        public T[] ToArrayAndFree()
        {
            var arr = _items.ToArray();
            _items.Clear();
            return arr;
        }

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    }

    /// <summary>
    /// Lightweight stand-in for the real PooledStringBuilder used by CaseInsensitiveComparison.ToLower.
    /// </summary>
    internal sealed class PooledStringBuilder
    {
        public StringBuilder Builder { get; } = new();

        public static PooledStringBuilder GetInstance() => new();

        public string ToStringAndFree() => Builder.ToString();
    }
}
