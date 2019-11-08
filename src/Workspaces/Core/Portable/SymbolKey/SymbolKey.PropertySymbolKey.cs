// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

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
                var containingTypeResolution = reader.ReadSymbolKey();
                var isIndexer = reader.ReadBoolean();

                using var refKinds = reader.ReadRefKindArray();
                using var parameterTypes = reader.ReadSymbolKeyArray<ITypeSymbol>();

                if (parameterTypes.IsDefault)
                {
                    return default;
                }

                using var properties = GetMembersOfNamedType<IPropertySymbol>(containingTypeResolution, metadataNameOpt: null);
                using var result = PooledArrayBuilder<IPropertySymbol>.GetInstance();
                foreach (var property in properties)
                {
                    if (property is { Parameters: { Length: refKinds.Count }, MetadataName: metadataName, IsIndexer: isIndexer } && ParameterRefKindsMatch(property.OriginalDefinition.Parameters, refKinds) && reader.ParameterTypesMatch(property.OriginalDefinition.Parameters, parameterTypes))
                    {
                        result.AddIfNotNull(property);
                    }
                }

                return CreateResolution(result);
            }
        }
    }
}
