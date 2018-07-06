// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial struct SymbolKey
    {
        private static class NamedTypeSymbolKey
        {
            public static void Create(INamedTypeSymbol symbol, SymbolKeyWriter writer)
            {
                writer.WriteString(symbol.MetadataName);
                writer.WriteSymbolKey(symbol.ContainingSymbol);
                writer.WriteInteger(symbol.Arity);
                writer.WriteInteger((int)symbol.TypeKind);
                writer.WriteBoolean(symbol.IsUnboundGenericType);

                if (!symbol.Equals(symbol.ConstructedFrom) && !symbol.IsUnboundGenericType)
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
                var metadataName = reader.ReadString();
                var resolvedContainingSymbol = reader.ReadSymbolKey();
                var arity = reader.ReadInteger();
                var typeKind = (TypeKind)reader.ReadInteger();
                var isUnboundGenericType = reader.ReadBoolean();
                var resolvedTypeArguments = reader.ReadSymbolKeyArray();

                var types = GetNamedTypeSymbols(reader, metadataName, resolvedContainingSymbol, arity, typeKind, isUnboundGenericType, resolvedTypeArguments);

                return ResolvedSymbolInfo.Create(types);
            }

            private static ImmutableArray<INamedTypeSymbol> GetNamedTypeSymbols(SymbolKeyReader reader, string metadataName, ResolvedSymbolInfo resolvedContainingSymbol, int arity, TypeKind typeKind, bool isUnboundGenericType, ImmutableArray<ResolvedSymbolInfo> resolvedTypeArguments)
            {
                var result = ArrayBuilder<INamedTypeSymbol>.GetInstance();

                foreach (var containingSymbol in resolvedContainingSymbol.GetAllSymbols<INamespaceOrTypeSymbol>())
                {
                    var backtickIndex = metadataName.IndexOf('`');
                    if (backtickIndex > 0)
                    {
                        metadataName = metadataName.Substring(0, backtickIndex);
                    }

                    var types = containingSymbol.GetTypeMembers(metadataName, arity);
                    var constructedTypes = ConstructTypes(types, resolvedTypeArguments, arity);

                    if (isUnboundGenericType)
                    {
                        result.AddRange(result.SelectAsArray(t => t.ConstructUnboundGenericType()));
                    }
                    else
                    {
                        result.AddRange(constructedTypes);
                    }
                }

                return result.ToImmutableAndFree();
            }
        }
    }
}
