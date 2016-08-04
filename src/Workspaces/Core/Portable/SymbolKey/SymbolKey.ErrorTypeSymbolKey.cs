// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

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
                    visitor.WriteSymbolKeyArray(default(ImmutableArray<ITypeSymbol>));
                }
            }

            public static int GetHashCode(GetHashCodeReader reader)
            {
                return Hash.Combine(reader.ReadString(),
                       Hash.Combine(reader.ReadSymbolKey(),
                       Hash.Combine(reader.ReadInteger(),
                                    reader.ReadSymbolKeyArrayHashCode())));
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var name = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();
                var arity = reader.ReadInteger();
                var typeArgumentResolutions = reader.ReadSymbolKeyArray();

                var errorTypes = ResolveErrorTypes(reader, containingSymbolResolution, name, arity);

                if (typeArgumentResolutions.IsDefault)
                {
                    return CreateSymbolInfo(errorTypes);
                }

                var typeArguments = typeArgumentResolutions.Select(
                    r => GetFirstSymbol<ITypeSymbol>(r)).ToArray();
                if (typeArguments.Any(s_typeIsNull))
                {
                    return default(SymbolKeyResolution);
                }

                return CreateSymbolInfo(errorTypes.Select(t => t.Construct(typeArguments)));
            }

            private static IEnumerable<INamedTypeSymbol> ResolveErrorTypes(
                SymbolKeyReader reader,
                SymbolKeyResolution containingSymbolResolution, string name, int arity)
            {
                if (containingSymbolResolution.GetAnySymbol() == null)
                {
                    yield return reader.Compilation.CreateErrorTypeSymbol(null, name, arity);
                }
                else
                {
                    foreach (var container in containingSymbolResolution.GetAllSymbols().OfType<INamespaceOrTypeSymbol>())
                    {
                        yield return reader.Compilation.CreateErrorTypeSymbol(container, name, arity);
                    }
                }
            }

        }
    }
}