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
                    visitor.WriteSymbolKeyArray(symbol.TypeArguments);
                }
                else
                {
                    visitor.WriteSymbolKeyArray(ImmutableArray<ITypeSymbol>.Empty);
                }
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();
                var arity = reader.ReadInteger();
                var isUnboundGenericType = reader.ReadBoolean();
                using var typeArguments = reader.ReadSymbolKeyArray<ITypeSymbol>();

                if (typeArguments.IsDefault)
                {
                    return default;
                }

                var typeArgumentArray = typeArguments.Count == 0
                    ? Array.Empty<ITypeSymbol>()
                    : typeArguments.Builder.ToArray();
                using var result = PooledArrayBuilder<INamedTypeSymbol>.GetInstance();
                foreach (var nsOrType in containingSymbolResolution.OfType<INamespaceOrTypeSymbol>())
                {
                    Resolve(
                        result, nsOrType, metadataName, arity,
                        isUnboundGenericType, typeArgumentArray);
                }

                return CreateResolution(result);
            }

            private static void Resolve(
                PooledArrayBuilder<INamedTypeSymbol> result,
                INamespaceOrTypeSymbol container,
                string metadataName,
                int arity,
                bool isUnboundGenericType,
                ITypeSymbol[] typeArguments)
            {
                foreach (var type in container.GetTypeMembers(GetName(metadataName), arity))
                {
                    var currentType = typeArguments.Length > 0 ? type.Construct(typeArguments) : type;
                    currentType = isUnboundGenericType ? currentType.ConstructUnboundGenericType() : currentType;

                    result.AddIfNotNull(currentType);
                }
            }
        }
    }
}
