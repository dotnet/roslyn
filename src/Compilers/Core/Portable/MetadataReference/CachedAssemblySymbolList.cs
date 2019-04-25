// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class CachedAssemblySymbolList
    {
        private readonly object _guard = new object();
        private readonly WeakList<IAssemblySymbol> _cachedSymbols = new WeakList<IAssemblySymbol>();

        internal void ForEach<T>(Action<T> action)
            where T : class, IAssemblySymbol
        {
            lock (_guard)
            {
                for (int i = 0; i < _cachedSymbols.WeakCount; i++)
                {
                    if (_cachedSymbols.GetWeakReference(i).TryGetTarget(out IAssemblySymbol assemblySymbol) &&
                        assemblySymbol is T typedSymbol)
                    {
                        action(typedSymbol);
                    }
                }
            }
        }

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
            lock (_guard)
            {
                for (int i = 0; i < _cachedSymbols.WeakCount; i++)
                {
                    if (_cachedSymbols.GetWeakReference(i).TryGetTarget(out IAssemblySymbol assemblySymbol))
                    {
                        list.Add(assemblySymbol);
                    }
                }
            }

            return list;
        }

    }
}
