// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class SpecializedSymbolCollections
    {
        public static PooledHashSet<TSymbol> GetPooledSymbolHashSetInstance<TSymbol>() where TSymbol : Symbol
        {
            var instance = PooledSymbolHashSet<TSymbol>.s_poolInstance.Allocate();
            Debug.Assert(instance.Count == 0);
            return instance;
        }

        private static class PooledSymbolHashSet<TSymbol> where TSymbol : Symbol
        {
            internal static readonly ObjectPool<PooledHashSet<TSymbol>> s_poolInstance = PooledHashSet<TSymbol>.CreatePool(SymbolEqualityComparer.ConsiderEverything);
        }

        public static PooledDictionary<KSymbol, V> GetPooledSymbolDictionaryInstance<KSymbol, V>() where KSymbol : Symbol
        {
            var instance = PooledSymbolDictionary<KSymbol, V>.s_poolInstance.Allocate();
            Debug.Assert(instance.Count == 0);
            return instance;
        }

        private static class PooledSymbolDictionary<TSymbol, V> where TSymbol : Symbol
        {
            internal static readonly ObjectPool<PooledDictionary<TSymbol, V>> s_poolInstance = PooledDictionary<TSymbol, V>.CreatePool(SymbolEqualityComparer.ConsiderEverything);
        }
    }
}
