// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// This is a collection of weakly held <see cref="IAssemblySymbol"/> instances. 
    ///
    /// The methods on this type can be safely called from multiple threads without risk of 
    /// corrupting the internal data structures.
    /// </summary>
    internal sealed class CachedAssemblySymbolList
    {
        private readonly object _guard = new object();
        private readonly WeakList<IAssemblySymbol> _cachedSymbols = new WeakList<IAssemblySymbol>();

        internal void ForEach<TSymbol, TData>(Action<TSymbol, TData> action, TData data)
            where TSymbol : class, IAssemblySymbol
        {
            lock (_guard)
            {
                for (int i = 0; i < _cachedSymbols.WeakCount; i++)
                {
                    if (_cachedSymbols.GetWeakReference(i).TryGetTarget(out IAssemblySymbol assemblySymbol) &&
                        assemblySymbol is TSymbol typedSymbol)
                    {
                        action(typedSymbol, data);
                    }
                }
            }
        }

        internal void ForEach<TData>(Action<IAssemblySymbol, TData> action, TData data) =>
            ForEach<IAssemblySymbol, TData>(action, data);

        internal void Add(IAssemblySymbol peAssembly)
        {
            lock (_guard)
            {
                _cachedSymbols.Add(peAssembly);
            }
        }

        internal List<IAssemblySymbol> CopyAll()
        {
            var list = new List<IAssemblySymbol>();
            ForEach((s, l) => l.Add(s), list);
            return list;
        }
    }
}
