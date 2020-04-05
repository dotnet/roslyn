// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

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

                // The 'false' section contains the logic to support resolving a forwarded type against the current
                // compilation.  This can be used in multi-targetting scenarios to find types that are in one location
                // in project to where they now may be found in another.
#if false
                return CreateResolution(result);
#else
                if (result.Count != 0)
                    return CreateResolution(result);

                // We couldn't resolve the type as it was encoded originally.  It's possible that this type got
                // forwarded to another dll in this compilation.
                var fullTypeName = GetFullMetadataName(containingSymbolResolution.GetAnySymbol(), metadataName);
                foreach (var assembly in reader.Compilation.GetReferencedAssemblySymbols())
                {
                    var type = assembly.ResolveForwardedType(fullTypeName);
                    if (type?.ContainingSymbol is INamespaceOrTypeSymbol nsOrType)
                        Resolve(
                            result, nsOrType, metadataName, arity,
                            isUnboundGenericType, typeArgumentArray);
                }

                return CreateResolution(result);
#endif
            }

            private static string GetFullMetadataName(ISymbol container, string metadataName)
            {
                using var _ = PooledStringBuilder.GetInstance(out var builder);
                Append(container, builder);
                if (builder.Length > 0)
                    builder.Append('.');

                builder.Append(metadataName);

                return builder.ToString();

                static void Append(ISymbol container, StringBuilder builder)
                {
                    if (!(container is INamespaceSymbol) && !(container is INamedTypeSymbol))
                        return;

                    Append(container.ContainingSymbol, builder);

                    if (container.MetadataName == "")
                        return;

                    if (builder.Length > 0)
                        builder.Append('.');

                    builder.Append(container.MetadataName);
                }
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
