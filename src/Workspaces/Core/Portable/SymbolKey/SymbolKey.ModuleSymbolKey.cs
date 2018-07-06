// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial struct SymbolKey
    {
        private static class ModuleSymbolKey
        {
            public static void Create(IModuleSymbol symbol, SymbolKeyWriter writer)
            {
                writer.WriteSymbolKey(symbol.ContainingSymbol);
            }

            public static ResolvedSymbolInfo Resolve(SymbolKeyReader reader)
            {
                var resolvedContainingSymbol = reader.ReadSymbolKey();

                var modules = GetModuleSymbols(resolvedContainingSymbol);

                return ResolvedSymbolInfo.Create(modules);
            }

            private static ImmutableArray<IModuleSymbol> GetModuleSymbols(ResolvedSymbolInfo resolvedContainingSymbol)
            {
                var result = ArrayBuilder<IModuleSymbol>.GetInstance();

                foreach (var assemblySymbol in resolvedContainingSymbol.GetAllSymbols<IAssemblySymbol>())
                {
                    // Don't check ModuleIds for equality because in practice, no-one uses them,
                    // and there is no way to set netmodule name programmatically using Roslyn
                    result.AddRange(assemblySymbol.Modules);
                }

                return result.ToImmutableAndFree();
            }
        }
    }
}
