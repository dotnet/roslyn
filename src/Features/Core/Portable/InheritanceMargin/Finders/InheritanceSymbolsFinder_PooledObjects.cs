// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InheritanceMargin.Finders
{
    internal partial class InheritanceSymbolsFinder
    {
        protected static readonly ObjectPool<PooledHashSet<ISymbol>> s_symbolHashSetPool = PooledHashSet<ISymbol>.CreatePool(MetadataUnifyingEquivalenceComparer.Instance);

        protected static readonly ObjectPool<PooledDictionary<ISymbol, PooledHashSet<ISymbol>>> s_symbolHashSetDictionary = PooledDictionary<ISymbol, PooledHashSet<ISymbol>>.CreatePool(MetadataUnifyingEquivalenceComparer.Instance);

        protected static SymbolSetDictionaryDisposer GetPooledHashSetDictionary(out PooledDictionary<ISymbol, PooledHashSet<ISymbol>> instance)
        {
            instance = s_symbolHashSetDictionary.Allocate();
            return new SymbolSetDictionaryDisposer(instance);
        }

        [NonCopyable]
        protected struct SymbolSetDictionaryDisposer : IDisposable
        {
            private readonly PooledDictionary<ISymbol, PooledHashSet<ISymbol>> pooledObject;

            public SymbolSetDictionaryDisposer(PooledDictionary<ISymbol, PooledHashSet<ISymbol>> pooledObject)
            {
                this.pooledObject = pooledObject;
            }

            public void Dispose()
            {
                foreach (var value in pooledObject.Values)
                {
                    value.Free();
                }

                pooledObject.Free();
            }
        }
    }
}
