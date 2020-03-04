// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                foreach (var container in containingSymbolResolution.OfType<INamespaceOrTypeSymbol>())
                {
                    result.AddIfNotNull(Construct(
                        reader, container, name, arity, typeArgumentsArray));
                }

                // Always ensure at least one error type was created.
                if (result.Count == 0)
                {
                    result.AddIfNotNull(Construct(
                        reader, container: null, name, arity, typeArgumentsArray));
                }

                return CreateResolution(result);
            }

            private static INamedTypeSymbol Construct(SymbolKeyReader reader, INamespaceOrTypeSymbol container, string name, int arity, ITypeSymbol[] typeArguments)
            {
                var result = reader.Compilation.CreateErrorTypeSymbol(container, name, arity);
                return typeArguments != null ? result.Construct(typeArguments) : result;
            }
        }
    }
}
