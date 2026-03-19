// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private readonly ref struct PooledArrayBuilder<T>
    {
        public readonly ArrayBuilder<T> Builder;

        private PooledArrayBuilder(ArrayBuilder<T> builder)
            => Builder = builder;

        public bool IsDefault => Builder == null;
        public int Count => Builder.Count;
        public T this[int index] => Builder[index];

        public void AddIfNotNull(T? value)
        {
            if (value != null)
            {
                Builder.Add(value);
            }
        }

        public void Dispose() => Builder?.Free();

        public ImmutableArray<T> ToImmutable() => Builder.ToImmutable();

        public ArrayBuilder<T>.Enumerator GetEnumerator() => Builder.GetEnumerator();

        public static PooledArrayBuilder<T> GetInstance()
            => new(ArrayBuilder<T>.GetInstance());

        public static PooledArrayBuilder<T> GetInstance(int capacity)
            => new(ArrayBuilder<T>.GetInstance(capacity));

        public void AddValuesIfNotNull(IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                AddIfNotNull(value);
            }
        }

        public void AddValuesIfNotNull(ImmutableArray<T> values)
        {
            foreach (var value in values)
            {
                AddIfNotNull(value);
            }
        }
    }
}
