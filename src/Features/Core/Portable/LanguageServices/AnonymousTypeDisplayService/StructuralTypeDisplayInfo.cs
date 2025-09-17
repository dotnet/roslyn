// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.LanguageService;

internal readonly struct StructuralTypeDisplayInfo(
    IDictionary<INamedTypeSymbol, string> structuralTypeToName,
    ImmutableArray<SymbolDisplayPart> typesParts)
{
    public static readonly StructuralTypeDisplayInfo Empty = default;

    public IDictionary<INamedTypeSymbol, string> StructuralTypeToName => structuralTypeToName ?? SpecializedCollections.EmptyDictionary<INamedTypeSymbol, string>();
    public ImmutableArray<SymbolDisplayPart> TypesParts => typesParts.NullToEmpty();

    public ImmutableArray<SymbolDisplayPart> ReplaceStructuralTypes(ImmutableArray<SymbolDisplayPart> parts, SemanticModel semanticModel, int position)
        => ReplaceStructuralTypes(parts, StructuralTypeToName, semanticModel, position);

    public static ImmutableArray<SymbolDisplayPart> ReplaceStructuralTypes(
        ImmutableArray<SymbolDisplayPart> parts,
        IDictionary<INamedTypeSymbol, string> structuralTypeToName,
        SemanticModel semanticModel,
        int position)
    {
        // Keep replacing parts until no more changes happen. 
        while (ReplaceStructuralTypes(parts, structuralTypeToName, semanticModel, position, out var newParts))
            parts = newParts;

        return parts;
    }

    public static bool ReplaceStructuralTypes(
        ImmutableArray<SymbolDisplayPart> parts,
        IDictionary<INamedTypeSymbol, string> structuralTypeToName,
        SemanticModel semanticModel,
        int position,
        out ImmutableArray<SymbolDisplayPart> newParts)
    {
        var changed = false;
        using var _ = ArrayBuilder<SymbolDisplayPart>.GetInstance(out var newPartsBuilder);

        foreach (var part in parts)
        {
            if (part.Symbol is INamedTypeSymbol type)
            {
                if (structuralTypeToName.TryGetValue(type, out var name) &&
                    part.ToString() != name)
                {
                    newPartsBuilder.Add(new SymbolDisplayPart(part.Kind, symbol: null, name));
                    changed = true;
                    continue;
                }

                // Expand out any tuples we're not placing in the Structural Type section.
                if (type.IsTupleType && part.ToString() == "<tuple>")
                {
                    var displayParts = type.ToMinimalDisplayParts(semanticModel, position);
                    newPartsBuilder.AddRange(displayParts);
                    changed = true;
                    continue;
                }
            }

            newPartsBuilder.Add(part);
        }

        if (!changed)
        {
            newParts = [];
            return false;
        }

        newParts = newPartsBuilder.ToImmutableAndClear();
        return true;
    }
}
