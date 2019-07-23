// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private ref struct PooledArrayBuilder<T>
        {
            public readonly ArrayBuilder<T> Builder;

            private PooledArrayBuilder(ArrayBuilder<T> builder)
                => Builder = builder;

            public bool IsDefault => Builder == null;
            public int Count => Builder.Count;
            public T this[int index] => Builder[index];

            public void AddIfNotNull(T value)
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
                => new PooledArrayBuilder<T>(ArrayBuilder<T>.GetInstance());

            public static PooledArrayBuilder<T> GetInstance(int capacity)
                => new PooledArrayBuilder<T>(ArrayBuilder<T>.GetInstance(capacity));

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
}
