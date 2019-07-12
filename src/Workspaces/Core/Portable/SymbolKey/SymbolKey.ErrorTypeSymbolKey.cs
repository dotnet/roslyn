// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class ErrorTypeSymbolKey
        {
            public static void Create(INamedTypeSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteString(symbol.Name);
                visitor.WriteSymbolKey(symbol.ContainingSymbol as INamespaceOrTypeSymbol);
                visitor.WriteInteger(symbol.Arity);

                if (!symbol.Equals(symbol.ConstructedFrom))
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
                var name = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();
                var arity = reader.ReadInteger();

                using var typeArguments = reader.ReadSymbolKeyArray<ITypeSymbol>();
                if (typeArguments.IsDefault)
                {
                    return default;
                }

                using var result = PooledArrayBuilder<INamedTypeSymbol>.GetInstance();

                var typeArgumentsArray = arity > 0 ? typeArguments.Builder.ToArray() : null;
                foreach (var container in containingSymbolResolution)
                {
                    if (container is INamespaceOrTypeSymbol containerTypeOrNS)
                    {
                        result.AddIfNotNull(Construct(
                            reader, containerTypeOrNS, name, arity, typeArgumentsArray));
                    }
                }

                // Always ensure at least one error type was created.
                if (result.Count == 0)
                {
                    result.AddIfNotNull(Construct(
                        reader, containerTypeOrNS: null, name, arity, typeArgumentsArray));
                }

                return CreateSymbolInfo(result);
            }

            private static INamedTypeSymbol Construct(SymbolKeyReader reader, INamespaceOrTypeSymbol containerTypeOrNS, string name, int arity, ITypeSymbol[] typeArguments)
            {
                var result = reader.Compilation.CreateErrorTypeSymbol(containerTypeOrNS, name, arity);
                return typeArguments != null ? result.Construct(typeArguments) : result;
            }
        }
    }
}
