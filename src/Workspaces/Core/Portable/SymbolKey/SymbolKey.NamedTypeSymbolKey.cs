// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class NamedTypeSymbolKey
        {
            public static void Create(INamedTypeSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteString(symbol.MetadataName);
                visitor.WriteSymbolKey(symbol.ContainingSymbol);
                visitor.WriteInteger(symbol.Arity);
                visitor.WriteBoolean(symbol.IsUnboundGenericType);

                if (!symbol.Equals(symbol.ConstructedFrom) && !symbol.IsUnboundGenericType)
                {
                    visitor.WriteBoolean(/*instantiate*/true);
                    visitor.WriteSymbolKeyArray(symbol.TypeArguments);
                }
                else
                {
                    visitor.WriteBoolean(/*instantiate*/false);
                    visitor.WriteSymbolKeyArray(ImmutableArray<ITypeSymbol>.Empty);
                }
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();
                var arity = reader.ReadInteger();
                var isUnboundGenericType = reader.ReadBoolean();
                var instantiate = reader.ReadBoolean();
                using var typeArguments = reader.ReadSymbolArray<ITypeSymbol>();

                if (instantiate && arity != typeArguments.Count)
                {
                    return default;
                }

                var typeArgumentArray = typeArguments.Count == 0
                    ? Array.Empty<ITypeSymbol>()
                    : typeArguments.Builder.ToArray();
                using var result = PooledArrayBuilder<INamedTypeSymbol>.GetInstance();
                foreach (var resolution in containingSymbolResolution)
                {
                    if (resolution is INamespaceOrTypeSymbol nsOrType)
                    {
                        Resolve(
                            result, nsOrType, metadataName, arity,
                            isUnboundGenericType, instantiate, typeArgumentArray);
                    }
                }

                return CreateSymbolInfo(result);
            }

            private static void Resolve(
                PooledArrayBuilder<INamedTypeSymbol> result,
                INamespaceOrTypeSymbol container,
                string metadataName,
                int arity,
                bool isUnboundGenericType,
                bool instantiate,
                ITypeSymbol[] typeArguments)
            {
                foreach (var type in container.GetTypeMembers(GetName(metadataName), arity))
                {
                    var currentType = instantiate ? type.Construct(typeArguments) : type;
                    currentType = isUnboundGenericType ? currentType.ConstructUnboundGenericType() : currentType;

                    result.AddIfNotNull(currentType);
                }
            }
        }
    }
}
