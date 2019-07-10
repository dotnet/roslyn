// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
                    visitor.WriteSymbolKeyArray(default(ImmutableArray<ITypeSymbol>));
                }
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();
                var arity = reader.ReadInteger();
                var isUnboundGenericType = reader.ReadBoolean();
                var typeArgumentsOpt = reader.ReadSymbolKeyArray();

                var types = GetAllSymbols<INamespaceOrTypeSymbol>(containingSymbolResolution).SelectMany(
                    s => Resolve(s, metadataName, arity, isUnboundGenericType, typeArgumentsOpt));
                return CreateSymbolInfo(types);
            }
            private static IEnumerable<INamedTypeSymbol> Resolve(
                INamespaceOrTypeSymbol container,
                string metadataName,
                int arity,
                bool isUnboundGenericType,
                ImmutableArray<SymbolKeyResolution> typeArguments)
            {
                var types = container.GetTypeMembers(GetName(metadataName), arity);
                var result = InstantiateTypes(types, arity, typeArguments);

                return isUnboundGenericType
                    ? result.Select(t => t.ConstructUnboundGenericType())
                    : result;
            }
        }
    }
}
