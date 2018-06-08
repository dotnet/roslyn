// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

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

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var name = reader.ReadString();
                var resolvedContainer = reader.ReadSymbolKey();
                var arity = reader.ReadInteger();
                var resolvedTypeArguments = reader.ReadSymbolKeyArray();

                var errorTypes = ResolveErrorTypes(reader, resolvedContainer, name, arity);

                if (resolvedTypeArguments.IsDefault)
                {
                    return SymbolKeyResolution.Create(errorTypes);
                }

                var typeArguments = resolvedTypeArguments
                    .SelectAsArray(r => r.GetFirstSymbol<ITypeSymbol>())
                    .ToArray();

                if (typeArguments.Any(s_typeIsNull))
                {
                    return default;
                }

                return SymbolKeyResolution.Create(errorTypes.SelectAsArray(t => t.Construct(typeArguments)));
            }

            private static ImmutableArray<INamedTypeSymbol> ResolveErrorTypes(
                SymbolKeyReader reader, SymbolKeyResolution resolvedContainer, string name, int arity)
            {
                var result = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

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

                return result.ToImmutable();
            }
        }
    }
}
