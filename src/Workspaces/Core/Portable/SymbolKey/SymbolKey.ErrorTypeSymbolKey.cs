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

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var name = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();
                var arity = reader.ReadInteger();
                var typeArgumentResolutions = reader.ReadSymbolKeyArray();

                var errorTypes = ResolveErrorTypes(reader, containingSymbolResolution, name, arity);

                if (typeArgumentResolutions.IsDefault)
                {
                    return SymbolKeyResolution.Create(errorTypes);
                }

                var typeArguments = typeArgumentResolutions.Select(
                    r => r.GetFirstSymbol<ITypeSymbol>()).ToArray();
                if (typeArguments.Any(s_typeIsNull))
                {
                    return default;
                }

                return SymbolKeyResolution.Create(errorTypes.Select(t => t.Construct(typeArguments)));
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
                    foreach (var container in containingSymbolResolution.GetAllSymbols<INamespaceOrTypeSymbol>())
                    {
                        yield return reader.Compilation.CreateErrorTypeSymbol(container, name, arity);
                    }
                }
            }

        }
    }
}
