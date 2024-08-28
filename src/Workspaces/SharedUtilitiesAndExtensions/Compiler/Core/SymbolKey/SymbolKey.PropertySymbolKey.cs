// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private sealed class PropertySymbolKey : AbstractSymbolKey<IPropertySymbol>
    {
        public static readonly PropertySymbolKey Instance = new();

        public sealed override void Create(IPropertySymbol symbol, SymbolKeyWriter visitor)
        {
            visitor.WriteString(symbol.MetadataName);
            visitor.WriteSymbolKey(symbol.ContainingSymbol);
            visitor.WriteBoolean(symbol.IsIndexer);
            visitor.WriteBoolean(symbol.PartialDefinitionPart != null);
            visitor.WriteRefKindArray(symbol.Parameters);
            visitor.WriteParameterTypesArray(symbol.OriginalDefinition.Parameters);
        }

        protected sealed override SymbolKeyResolution Resolve(
            SymbolKeyReader reader, IPropertySymbol? contextualSymbol, out string? failureReason)
        {
            var metadataName = reader.ReadString();

            var containingTypeResolution = reader.ReadSymbolKey(contextualSymbol?.ContainingSymbol, out var containingTypeFailureReason);

            var isIndexer = reader.ReadBoolean();
            var isPartialImplementationPart = reader.ReadBoolean();
            using var refKinds = reader.ReadRefKindArray();

            using var properties = GetMembersOfNamedType<IPropertySymbol>(containingTypeResolution, metadataName: null);
            using var result = PooledArrayBuilder<IPropertySymbol>.GetInstance();

            // For each property that we look at, we'll have to resolve the parameter list and return type in the
            // context of that method.  This makes sure we can attempt to resolve the parameter list types against
            // error types in the property we're currently looking at.
            //
            // Because of this, we keep track of where we are in the reader.  Before resolving every parameter list,
            // we'll mark which method we're on and we'll rewind to this point.
            var beforeParametersPosition = reader.Position;

            IPropertySymbol? property = null;
            foreach (var candidate in properties)
            {
                if (candidate.Parameters.Length != refKinds.Count ||
                    candidate.MetadataName != metadataName ||
                    candidate.IsIndexer != isIndexer ||
                    !ParameterRefKindsMatch(candidate.OriginalDefinition.Parameters, refKinds))
                {
                    continue;
                }

                property = Resolve(reader, isPartialImplementationPart, candidate);
                if (property != null)
                    break;

                // reset ourselves so we can check the return-type/parameters against the next candidate.
                reader.Position = beforeParametersPosition;
            }

            if (reader.Position == beforeParametersPosition)
            {
                // We didn't find a match.  Read through the stream one final time so we're at the correct location
                // after this PropertySymbolKey.

                _ = reader.ReadSymbolKeyArray<IPropertySymbol, ITypeSymbol>(
                    contextualSymbol: null, getContextualSymbol: null, failureReason: out _);
            }

            if (containingTypeFailureReason != null)
            {
                failureReason = $"({nameof(PropertySymbolKey)} {nameof(containingTypeResolution)} failed -> {containingTypeFailureReason})";
                return default;
            }

            if (property == null)
            {
                failureReason = $"({nameof(PropertySymbolKey)} '{metadataName}' not found)";
                return default;
            }

            failureReason = null;
            return new SymbolKeyResolution(property);
        }

        private static IPropertySymbol? Resolve(
            SymbolKeyReader reader,
            bool isPartialImplementationPart,
            IPropertySymbol property)
        {
            if (reader.ParameterTypesMatch(
                    property,
                    getContextualType: static (property, i) => SafeGet(property.OriginalDefinition.Parameters, i)?.Type,
                    property.OriginalDefinition.Parameters))
            {
                if (isPartialImplementationPart)
                    property = property.PartialImplementationPart ?? property;

                Debug.Assert(property != null);
                return property;
            }

            return null;
        }
    }
}
