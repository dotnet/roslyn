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
                foreach (var kvp in groupMap)
                {
                    var parts = kvp.Value;
                    var updatedParts = anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parts, semanticModel, position, this.displayService);
                    if (parts != updatedParts)
                    {
                        groupMap[kvp.Key] = updatedParts;
                        goto restart;
                    }
                }
            }

            private void FixNormalAnonymousTypes(ISymbol firstSymbol)
            {
                var directNormalAnonymousTypeReferences =
                    from parts in groupMap.Values
                    from part in parts
                    where part.Symbol.IsNormalAnonymousType()
                    select (INamedTypeSymbol)part.Symbol;

                var info = anonymousTypeDisplayService.GetNormalAnonymousTypeDisplayInfo(
                    firstSymbol, directNormalAnonymousTypeReferences, semanticModel, position, this.displayService);

                if (info.AnonymousTypesParts.Count > 0)
                {
                    AddToGroup(SymbolDescriptionGroups.AnonymousTypes,
                        info.AnonymousTypesParts);

                restart:
                    foreach (var kvp in groupMap)
                    {
                        var parts = groupMap[kvp.Key];
                        var updatedParts = info.ReplaceAnonymousTypes(parts);
                        if (parts != updatedParts)
                        {
                            groupMap[kvp.Key] = updatedParts;
                            goto restart;
                        }
                    }
                }
            }
        }
    }
}
