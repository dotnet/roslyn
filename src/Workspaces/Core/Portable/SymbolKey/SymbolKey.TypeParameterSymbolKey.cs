// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial struct SymbolKey
    {
        private static class TypeParameterSymbolKey
        {
            public static void Create(ITypeParameterSymbol symbol, SymbolKeyWriter writer)
            {
                writer.WriteString(symbol.MetadataName);
                writer.WriteSymbolKey(symbol.ContainingSymbol);
            }

            public static ResolvedSymbolInfo Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var resolvedContainingSymbol = reader.ReadSymbolKey();

                var result = GetTypeParameterSymbols(metadataName, resolvedContainingSymbol, reader.Compilation);
                return ResolvedSymbolInfo.Create(result);
            }

            private static ImmutableArray<ITypeParameterSymbol> GetTypeParameterSymbols(string metadataName, ResolvedSymbolInfo resolvedContainingSymbol, Compilation compilation)
            {
                var result = ArrayBuilder<ITypeParameterSymbol>.GetInstance();

                foreach (var container in resolvedContainingSymbol.GetAllSymbols())
                {
                    switch (container)
                    {
                        case INamedTypeSymbol namedTypeSymbol:
                            AddTypeParameters(namedTypeSymbol.TypeParameters);
                            break;
                        case IMethodSymbol methodSymbol:
                            AddTypeParameters(methodSymbol.TypeParameters);
                            break;
                    }
                }

                void AddTypeParameters(ImmutableArray<ITypeParameterSymbol> typeParameters)
                {
                    // TODO(dustinca): Should we check case-insensitively for VB here?
                    foreach (var typeParameter in typeParameters)
                    {
                        if (NamesAreEqual(compilation, typeParameter.MetadataName, metadataName))
                        {
                            result.Add(typeParameter);
                        }
                    }
                }

                return result.ToImmutableAndFree();
            }
        }
    }
}
