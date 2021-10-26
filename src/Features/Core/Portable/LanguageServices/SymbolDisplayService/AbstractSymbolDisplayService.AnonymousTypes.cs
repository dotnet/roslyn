// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal partial class AbstractSymbolDisplayService
    {
        protected abstract partial class AbstractSymbolDescriptionBuilder
        {
            private void FixAllStructuralTypes(ISymbol firstSymbol)
            {
                // First, inline all the delegate anonymous types.  This is how VB prefers to display
                // things.
                InlineAllDelegateAnonymousTypes();

                // Now, replace all normal anonymous types and tuples with 'a, 'b, etc. and create a
                // Structural Types: section to display their info.
                FixStructuralTypes(firstSymbol);
            }

            private void InlineAllDelegateAnonymousTypes()
            {
restart:
                foreach (var (group, parts) in _groupMap)
                {
                    var updatedParts = _structuralTypeDisplayService.InlineDelegateAnonymousTypes(parts, _semanticModel, _position);
                    if (parts != updatedParts)
                    {
                        _groupMap[group] = updatedParts;
                        goto restart;
                    }
                }
            }

            private void FixStructuralTypes(ISymbol firstSymbol)
            {
                var directStructuralTypes =
                    from parts in _groupMap.Values
                    from part in parts
                    where part.Symbol.IsNormalAnonymousType() || part.Symbol.IsTupleType()
                    select (INamedTypeSymbol)part.Symbol;

                var info = _structuralTypeDisplayService.GetTypeDisplayInfo(
                    firstSymbol, directStructuralTypes.ToImmutableArrayOrEmpty(), _semanticModel, _position);

                if (info.TypesParts.Count > 0)
                {
                    AddToGroup(SymbolDescriptionGroups.StructuralTypes, info.TypesParts);

                    foreach (var (group, parts) in _groupMap.ToArray())
                        _groupMap[group] = info.ReplaceStructuralTypes(parts, _semanticModel, _position);
                }
            }
        }
    }
}
