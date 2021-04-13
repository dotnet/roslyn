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
            private void FixAllAnonymousTypes(ISymbol firstSymbol)
            {
                // First, inline all the delegate anonymous types.  This is how VB prefers to display
                // things.
                InlineAllDelegateAnonymousTypes();

                // Now, replace all normal anonymous types with 'a, 'b, etc. and create a
                // AnonymousTypes: section to display their info.
                FixNormalAnonymousTypes(firstSymbol);
            }

            private void InlineAllDelegateAnonymousTypes()
            {
restart:
                foreach (var (group, parts) in _groupMap)
                {
                    var updatedParts = _anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parts, _semanticModel, _position);
                    if (parts != updatedParts)
                    {
                        _groupMap[group] = updatedParts;
                        goto restart;
                    }
                }
            }

            private void FixNormalAnonymousTypes(ISymbol firstSymbol)
            {
                var directNormalAnonymousTypeReferences =
                    from parts in _groupMap.Values
                    from part in parts
                    where part.Symbol.IsNormalAnonymousType()
                    select (INamedTypeSymbol)part.Symbol;

                var info = _anonymousTypeDisplayService.GetNormalAnonymousTypeDisplayInfo(
                    firstSymbol, directNormalAnonymousTypeReferences, _semanticModel, _position);

                if (info.AnonymousTypesParts.Count > 0)
                {
                    AddToGroup(SymbolDescriptionGroups.AnonymousTypes,
                        info.AnonymousTypesParts);

restart:
                    foreach (var (group, parts) in _groupMap)
                    {
                        var updatedParts = info.ReplaceAnonymousTypes(parts);
                        if (parts != updatedParts)
                        {
                            _groupMap[group] = updatedParts;
                            goto restart;
                        }
                    }
                }
            }
        }
    }
}
