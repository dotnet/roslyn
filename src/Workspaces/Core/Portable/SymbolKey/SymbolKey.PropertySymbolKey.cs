// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial struct SymbolKey
    {
        private static class PropertySymbolKey
        {
            public static void Create(IPropertySymbol symbol, SymbolKeyWriter writer)
            {
                writer.WriteString(symbol.MetadataName);
                writer.WriteSymbolKey(symbol.ContainingSymbol);
                writer.WriteBoolean(symbol.IsIndexer);
                writer.WriteRefKindArray(symbol.Parameters);
                writer.WriteParameterTypesArray(symbol.OriginalDefinition.Parameters);
            }

            public static ResolvedSymbolInfo Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var resolvedContainingSymbol = reader.ReadSymbolKey();
                var isIndexer = reader.ReadBoolean();
                var refKinds = reader.ReadRefKindArray();
                var originalParameterTypes = reader.ReadSymbolKeyArray()
                    .SelectAsArray(r => r.GetFirstSymbol<ITypeSymbol>());

                if (originalParameterTypes.Any(s_typeIsNull))
                {
                    return default;
                }

                var properties = GetPropertySymbols(reader, metadataName, resolvedContainingSymbol, isIndexer, refKinds, originalParameterTypes);

                return ResolvedSymbolInfo.Create(properties);
            }

            private static ImmutableArray<IPropertySymbol> GetPropertySymbols(
                SymbolKeyReader reader,
                string metadataName,
                ResolvedSymbolInfo resolvedContainingSymbol,
                bool isIndexer,
                ImmutableArray<RefKind> refKinds,
                ImmutableArray<ITypeSymbol> originalParameterTypes)
            {
                var result = ArrayBuilder<IPropertySymbol>.GetInstance();

                foreach (var containingType in resolvedContainingSymbol.GetAllSymbols<INamedTypeSymbol>())
                {
                    foreach (var member in containingType.GetMembers())
                    {
                        if (member.MetadataName != metadataName)
                        {
                            continue;
                        }

                        if (member is IPropertySymbol property)
                        {
                            if (property.IsIndexer == isIndexer)
                            {
                                var parameters = property.OriginalDefinition.Parameters;

                                if (!reader.ParameterRefKindsMatch(parameters, refKinds) ||
                                    !reader.ParameterTypesMatch(parameters, originalParameterTypes))
                                {
                                    continue;
                                }
                            }

                            result.Add(property);
                        }
                    }
                }

                return result.ToImmutableAndFree();
            }
        }
    }
}
