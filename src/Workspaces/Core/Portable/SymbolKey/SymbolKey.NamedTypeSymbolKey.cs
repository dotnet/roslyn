// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                var metadataName = reader.ReadString()!;
                var containingSymbolResolution = reader.ReadSymbolKey(out var containingSymbolFailureReason);
                var arity = reader.ReadInteger();
                var isUnboundGenericType = reader.ReadBoolean();
                using var typeArguments = reader.ReadSymbolKeyArray<ITypeSymbol>(out var typeArgumentsFailureReason);

                if (containingSymbolFailureReason != null)
                {
                    failureReason = $"({nameof(NamedTypeSymbolKey)} {nameof(containingSymbolFailureReason)} failed -> {containingSymbolFailureReason})";
                    return default;
                }

                if (typeArgumentsFailureReason != null)
                {
                    failureReason = $"({nameof(NamedTypeSymbolKey)} {nameof(typeArguments)} failed -> {typeArgumentsFailureReason})";
                    return default;
                }

                if (typeArguments.IsDefault)
                {
                    failureReason = $"({nameof(NamedTypeSymbolKey)} {nameof(typeArguments)} failed)";
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

                return CreateResolution(result, $"({nameof(NamedTypeSymbolKey)} failed)", out failureReason);
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
