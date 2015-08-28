// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                foreach (var kvp in _groupMap)
                {
                    var parts = kvp.Value;
                    var updatedParts = _anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parts, _semanticModel, _position, _displayService);
                    if (parts != updatedParts)
                    {
                        _groupMap[kvp.Key] = updatedParts;
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
                    firstSymbol, directNormalAnonymousTypeReferences, _semanticModel, _position, _displayService);

                if (info.AnonymousTypesParts.Count > 0)
                {
                    AddToGroup(SymbolDescriptionGroups.AnonymousTypes,
                        info.AnonymousTypesParts);

                restart:
                    foreach (var kvp in _groupMap)
                    {
                        var parts = _groupMap[kvp.Key];
                        var updatedParts = info.ReplaceAnonymousTypes(parts);
                        if (parts != updatedParts)
                        {
                            _groupMap[kvp.Key] = updatedParts;
                            goto restart;
                        }
                    }
                }
            }
        }
    }
}
