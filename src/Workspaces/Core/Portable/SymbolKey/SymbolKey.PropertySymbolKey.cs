// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class PropertySymbolKey
        {
            public static void Create(IPropertySymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteString(symbol.MetadataName);
                visitor.WriteSymbolKey(symbol.ContainingSymbol);
                visitor.WriteBoolean(symbol.IsIndexer);
                visitor.WriteRefKindArray(symbol.Parameters);
                visitor.WriteParameterTypesArray(symbol.OriginalDefinition.Parameters);
            }

            public static int GetHashCode(GetHashCodeReader reader)
            {
                return Hash.Combine(reader.ReadString(),
                       Hash.Combine(reader.ReadSymbolKey(),
                       Hash.Combine(reader.ReadBoolean(),
                       Hash.Combine(reader.ReadRefKindArrayHashCode(),
                                    reader.ReadSymbolKeyArrayHashCode()))));
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();
                var isIndexer = reader.ReadBoolean();
                var refKinds = reader.ReadRefKindArray();
                var originalParameterTypes = reader.ReadSymbolKeyArray().Select(
                    r => GetFirstSymbol<ITypeSymbol>(r)).ToArray();

                if (originalParameterTypes.Any(s_typeIsNull))
                {
                    return default(SymbolKeyResolution);
                }

                var properties = containingSymbolResolution.GetAllSymbols().OfType<INamedTypeSymbol>()
                    .SelectMany(t => t.GetMembers())
                    .OfType<IPropertySymbol>()
                    .Where(p => p.Parameters.Length == refKinds.Length &&
                                p.MetadataName == metadataName &&
                                p.IsIndexer == isIndexer);
                var matchingProperties = properties.Where(p =>
                    ParameterRefKindsMatch(p.OriginalDefinition.Parameters, refKinds) &&
                    reader.ParameterTypesMatch(p.OriginalDefinition.Parameters, originalParameterTypes));

                return CreateSymbolInfo(matchingProperties);
            }
        }
    }
}