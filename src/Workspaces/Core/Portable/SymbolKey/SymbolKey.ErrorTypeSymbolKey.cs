// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial struct SymbolKey
    {
        private static class ErrorTypeSymbolKey
        {
            public static void Create(INamedTypeSymbol symbol, SymbolKeyWriter writer)
            {
                writer.WriteString(symbol.Name);
                writer.WriteSymbolKey(symbol.ContainingSymbol as INamespaceOrTypeSymbol);
                writer.WriteInteger(symbol.Arity);

                if (!symbol.Equals(symbol.ConstructedFrom))
                {
                    writer.WriteSymbolKeyArray(symbol.TypeArguments);
                }
                else
                {
                    writer.WriteSymbolKeyArray(default(ImmutableArray<ITypeSymbol>));
                }
            }

            public static ResolvedSymbolInfo Resolve(SymbolKeyReader reader)
            {
                var name = reader.ReadString();
                var resolvedContainer = reader.ReadSymbolKey();
                var arity = reader.ReadInteger();
                var resolvedTypeArguments = reader.ReadSymbolKeyArray();

                var errorTypes = ResolveErrorTypes(reader, resolvedContainer, name, arity);
                var constructedTypes = ConstructTypes(errorTypes, resolvedTypeArguments, arity);

                return ResolvedSymbolInfo.Create(constructedTypes);
            }

            private static ImmutableArray<INamedTypeSymbol> ResolveErrorTypes(
                SymbolKeyReader reader, ResolvedSymbolInfo resolvedContainer, string name, int arity)
            {
                var result = ArrayBuilder<INamedTypeSymbol>.GetInstance();

                if (resolvedContainer.GetAnySymbol() == null)
                {
                    result.Add(reader.Compilation.CreateErrorTypeSymbol(null, name, arity));
                }
                else
                {
                    foreach (var container in resolvedContainer.GetAllSymbols<INamespaceOrTypeSymbol>())
                    {
                        result.Add(reader.Compilation.CreateErrorTypeSymbol(container, name, arity));
                    }
                }

                return result.ToImmutableAndFree();
            }
        }
    }
}
