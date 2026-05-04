// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.LanguageService;

internal readonly struct StructuralTypeDisplayInfo(
    IDictionary<INamedTypeSymbol, string> structuralTypeToName,
    IList<SymbolDisplayPart> typesParts)
{
    public IDictionary<INamedTypeSymbol, string> StructuralTypeToName { get; } = structuralTypeToName;
    public IList<SymbolDisplayPart> TypesParts { get; } = typesParts;

    public IList<SymbolDisplayPart> ReplaceStructuralTypes(IList<SymbolDisplayPart> parts, SemanticModel semanticModel, int position)
        => ReplaceStructuralTypes(parts, StructuralTypeToName, semanticModel, position);

    public static IList<SymbolDisplayPart> ReplaceStructuralTypes(
        IList<SymbolDisplayPart> parts,
        IDictionary<INamedTypeSymbol, string> structuralTypeToName,
        SemanticModel semanticModel,
        int position)
    {
        if (structuralTypeToName is null)
            return parts;

        // Keep replacing parts until no more changes happen. 
        while (ReplaceStructuralTypes(parts, structuralTypeToName, semanticModel, position, out var newParts))
            parts = newParts;

        return parts;
    }

    private static bool ReplaceStructuralTypes(
        IList<SymbolDisplayPart> parts,
        IDictionary<INamedTypeSymbol, string> structuralTypeToName,
        SemanticModel semanticModel,
        int position,
        out List<SymbolDisplayPart> newParts)
    {
        var changed = false;
        newParts = [];

        foreach (var part in parts)
        {
            if (part.Symbol is INamedTypeSymbol type)
            {
                if (structuralTypeToName.TryGetValue(type, out var name) &&
                    part.ToString() != name)
                {
                    newParts.Add(new SymbolDisplayPart(part.Kind, symbol: null, name));
                    changed = true;
                    continue;
                }

                // Expand out any tuples we're not placing in the Structural Type section.
                if (type.IsTupleType && part.ToString() == "<tuple>")
                {
                    var displayParts = type.ToMinimalDisplayParts(semanticModel, position);
                    newParts.AddRange(displayParts);
                    changed = true;
                    continue;
                }
            }

            newParts.Add(part);
        }

        return changed;
    }
}
