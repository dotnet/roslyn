// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal readonly struct StructuralTypeDisplayInfo
    {
        public IDictionary<INamedTypeSymbol, string> StructuralTypeToName { get; }
        public IList<SymbolDisplayPart> TypesParts { get; }

        public StructuralTypeDisplayInfo(
            IDictionary<INamedTypeSymbol, string> structuralTypeToName,
            IList<SymbolDisplayPart> typesParts)
            : this()
        {
            StructuralTypeToName = structuralTypeToName;
            TypesParts = typesParts;
        }

        public IList<SymbolDisplayPart> ReplaceStructuralTypes(IList<SymbolDisplayPart> parts, SemanticModel semanticModel, int position)
            => ReplaceStructuralTypes(parts, StructuralTypeToName, semanticModel, position);

        public static IList<SymbolDisplayPart> ReplaceStructuralTypes(
            IList<SymbolDisplayPart> parts,
            IDictionary<INamedTypeSymbol, string> structuralTypeToName,
            SemanticModel semanticModel,
            int position)
        {
            bool changed;
            do
            {
                changed = false;

                for (var i = 0; i < parts.Count; i++)
                {
                    var part = parts[i];
                    if (part.Symbol is INamedTypeSymbol type)
                    {
                        if (structuralTypeToName.TryGetValue(type, out var name) &&
                            part.ToString() != name)
                        {
                            parts[i] = new SymbolDisplayPart(part.Kind, part.Symbol, name);
                            changed = true;
                            continue;
                        }

                        // Expand out any tuples we're not placing in the Structural Type section.
                        if (type.IsTupleType && part.ToString() == "<tuple>")
                        {
                            parts.RemoveAt(i);
                            var displayParts = type.ToMinimalDisplayParts(semanticModel, position);
                            for (var n = displayParts.Length - 1; n >= 0; n--)
                                parts.Insert(i, displayParts[n]);
                            changed = true;
                            break;
                        }
                    }
                }
            } while (changed);

            return parts;
        }
    }
}
