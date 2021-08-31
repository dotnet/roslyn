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

        public IList<SymbolDisplayPart> ReplaceStructuralTypes(IList<SymbolDisplayPart> parts)
            => ReplaceStructuralTypes(parts, StructuralTypeToName);

        public static IList<SymbolDisplayPart> ReplaceStructuralTypes(
            IList<SymbolDisplayPart> parts,
            IDictionary<INamedTypeSymbol, string> structuralTypeToName)
        {
            var result = parts;
            for (var i = 0; i < result.Count; i++)
            {
                var part = result[i];
                if (part.Symbol is INamedTypeSymbol type && structuralTypeToName.TryGetValue(type, out var name) && part.ToString() != name)
                {
                    result = result == parts ? new List<SymbolDisplayPart>(parts) : result;
                    result[i] = new SymbolDisplayPart(part.Kind, part.Symbol, name);
                }
            }

            return result;
        }
    }
}
