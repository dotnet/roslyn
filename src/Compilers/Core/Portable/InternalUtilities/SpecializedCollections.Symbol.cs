// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    internal static partial class SpecializedCollections
    {
        public static PooledHashSet<ISymbol> GetPooledSymbolHashSetInstance<TSymbol>() where TSymbol : ISymbol
        {
            var instance = PooledSymbolHashSet<TSymbol>.s_poolInstance.Allocate();
            Debug.Assert(instance.Count == 0);
            return instance;
        }

        private static class PooledSymbolHashSet<TSymbol> where TSymbol : ISymbol
        {
            internal static readonly ObjectPool<PooledHashSet<ISymbol>> s_poolInstance = PooledHashSet<ISymbol>.CreatePool(SymbolEqualityComparer.ConsiderEverything);
        }

        public static PooledDictionary<ISymbol, V> GetPooledSymbolDictionaryInstance<KSymbol, V>() where KSymbol : ISymbol
        {
            var instance = PooledSymbolDictionary<KSymbol, V>.s_poolInstance.Allocate();
            Debug.Assert(instance.Count == 0);
            return instance;
        }

        private static class PooledSymbolDictionary<TSymbol, V> where TSymbol : ISymbol
        {
            internal static readonly ObjectPool<PooledDictionary<ISymbol, V>> s_poolInstance = PooledDictionary<ISymbol, V>.CreatePool(SymbolEqualityComparer.ConsiderEverything);
        }
    }
}
