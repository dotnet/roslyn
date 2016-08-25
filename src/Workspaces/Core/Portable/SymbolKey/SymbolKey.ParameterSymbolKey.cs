// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class ParameterSymbolKey
        {
            public static void Create(IParameterSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteString(symbol.MetadataName);
                visitor.WriteSymbolKey(symbol.ContainingSymbol);
            }

            public static int GetHashCode(GetHashCodeReader reader)
            {
                return Hash.Combine(reader.ReadString(), reader.ReadSymbolKey());
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();

                var parameters = GetAllSymbols(containingSymbolResolution).SelectMany(
                    s => Resolve(reader, s, metadataName));
                return CreateSymbolInfo(parameters);
            }

            private static IEnumerable<IParameterSymbol> Resolve(
                SymbolKeyReader reader, ISymbol container, string metadataName)
            {
                if (container is IMethodSymbol)
                {
                    return ((IMethodSymbol)container).Parameters.Where(
                        p => SymbolKey.Equals(reader.Compilation, p.MetadataName, metadataName));
                }
                else if (container is IPropertySymbol)
                {
                    return ((IPropertySymbol)container).Parameters.Where(
                        p => SymbolKey.Equals(reader.Compilation, p.MetadataName, metadataName));
                }
                else
                {
                    return SpecializedCollections.EmptyEnumerable<IParameterSymbol>();
                }
            }
        }
    }
}