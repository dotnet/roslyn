// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class TypeParameterSymbolKey
        {
            public static void Create(ITypeParameterSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteString(symbol.MetadataName);
                visitor.WriteSymbolKey(symbol.ContainingSymbol);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();

                var result = containingSymbolResolution.GetAllSymbols()
                    .SelectMany(s =>
                    {
                        if (s is INamedTypeSymbol namedType)
                        {
                            return namedType.TypeParameters.Where(p => p.MetadataName == metadataName);
                        }
                        else if (s is IMethodSymbol method)
                        {
                            return method.TypeParameters.Where(p => p.MetadataName == metadataName);
                        }
                        else
                        {
                            return SpecializedCollections.EmptyEnumerable<ITypeParameterSymbol>();
                        }
                    });
                return CreateSymbolInfo(result);
            }
        }
    }
}
