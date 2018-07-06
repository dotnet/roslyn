// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial struct SymbolKey
    {
        private static class PointerTypeSymbolKey
        {
            public static void Create(IPointerTypeSymbol symbol, SymbolKeyWriter writer)
            {
                writer.WriteSymbolKey(symbol.PointedAtType);
            }

            public static ResolvedSymbolInfo Resolve(SymbolKeyReader reader)
            {
                var resolvedPointedAtType = reader.ReadSymbolKey();

                var pointerTypeSymbols = GetPointerTypeSymbols(resolvedPointedAtType, reader.Compilation);
                return ResolvedSymbolInfo.Create(pointerTypeSymbols);
            }

            private static ImmutableArray<IPointerTypeSymbol> GetPointerTypeSymbols(ResolvedSymbolInfo resolvedPointedAtType, Compilation compilation)
            {
                var result = ArrayBuilder<IPointerTypeSymbol>.GetInstance();

                foreach (var pointedAtType in resolvedPointedAtType.GetAllSymbols<ITypeSymbol>())
                {
                    result.Add(compilation.CreatePointerTypeSymbol(pointedAtType));
                }

                return result.ToImmutableAndFree();
            }
        }
    }
}
