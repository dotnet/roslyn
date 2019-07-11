// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

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

                using var typeArguments = PooledArrayBuilder<ITypeSymbol>.GetInstance();
                using var errorTypes = PooledArrayBuilder<INamedTypeSymbol>.GetInstance();

                reader.FillSymbolArray(typeArguments);
                ResolveErrorTypes(errorTypes, reader, containingSymbolResolution, name, arity);

                if (typeArguments.Count != arity)
                {
                    return default;
                }

                if (arity == 0)
                {
                    return CreateSymbolInfo(errorTypes);
                }

                using var result = PooledArrayBuilder<INamedTypeSymbol>.GetInstance();
                var typeArgumentsArray = typeArguments.Builder.ToArray();
                foreach (var type in errorTypes)
                {
                    result.AddIfNotNull(type.Construct(typeArgumentsArray));
                }

                return CreateSymbolInfo(result);
            }

            private static void ResolveErrorTypes(
                PooledArrayBuilder<INamedTypeSymbol> errorTypes,
                SymbolKeyReader reader,
                SymbolKeyResolution containingSymbolResolution,
                string name, int arity)
            {
                if (containingSymbolResolution.GetAnySymbol() == null)
                {
                    errorTypes.AddIfNotNull(reader.Compilation.CreateErrorTypeSymbol(null, name, arity));
                }
                else
                {
                    foreach (var container in containingSymbolResolution)
                    {
                        if (container is INamespaceOrTypeSymbol containerTypeOrNS)
                        {
                            errorTypes.AddIfNotNull(reader.Compilation.CreateErrorTypeSymbol(containerTypeOrNS, name, arity));
                        }
                    }
                }
            }
        }
    }
}
