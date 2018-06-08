// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;

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

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();
                var isIndexer = reader.ReadBoolean();
                var refKinds = reader.ReadRefKindArray();
                var originalParameterTypes = reader.ReadSymbolKeyArray().Select(
                    r => r.GetFirstSymbol<ITypeSymbol>()).ToArray();

                if (originalParameterTypes.Any(s_typeIsNull))
                {
                    return default;
                }

                var properties = containingSymbolResolution.GetAllSymbols<INamedTypeSymbol>()
                    .SelectMany(t => t.GetMembers())
                    .OfType<IPropertySymbol>()
                    .Where(p => p.Parameters.Length == refKinds.Length &&
                                p.MetadataName == metadataName &&
                                p.IsIndexer == isIndexer);
                var matchingProperties = properties.Where(p =>
                    ParameterRefKindsMatch(p.OriginalDefinition.Parameters, refKinds) &&
                    reader.ParameterTypesMatch(p.OriginalDefinition.Parameters, originalParameterTypes));

                return SymbolKeyResolution.Create(matchingProperties);
            }
        }
    }
}
