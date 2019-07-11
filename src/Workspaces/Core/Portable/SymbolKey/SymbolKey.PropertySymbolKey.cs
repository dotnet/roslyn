// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();
                var isIndexer = reader.ReadBoolean();

                using var refKinds = reader.ReadRefKindArray();;
                using var parameterTypes = reader.ReadSymbolArray<ITypeSymbol>();

                if (refKinds.Count != parameterTypes.Count)
                {
                    return default;
                }

                using var result = PooledArrayBuilder<IPropertySymbol>.GetInstance();
                foreach (var containingSymbol in containingSymbolResolution)
                {
                    if (containingSymbol is INamedTypeSymbol containingNamedType)
                    {
                        foreach (var member in containingNamedType.GetMembers())
                        {
                            if (member is IPropertySymbol property)
                            {
                                if (property.Parameters.Length == refKinds.Count &&
                                    property.MetadataName == metadataName &&
                                    property.IsIndexer == isIndexer &&
                                    ParameterRefKindsMatch(property.OriginalDefinition.Parameters, refKinds) &&
                                    reader.ParameterTypesMatch(property.OriginalDefinition.Parameters, parameterTypes))
                                {
                                    result.AddIfNotNull(property);
                                }
                            }
                        }
                    }
                }

                return CreateSymbolInfo(result);
            }
        }
    }
}
