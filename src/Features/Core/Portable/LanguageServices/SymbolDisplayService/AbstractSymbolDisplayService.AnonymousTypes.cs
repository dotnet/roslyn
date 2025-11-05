// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageService;

internal abstract partial class AbstractSymbolDisplayService
{
    protected abstract partial class AbstractSymbolDescriptionBuilder
    {
        private StructuralTypeDisplayInfo FixAllStructuralTypes(ISymbol firstSymbol)
        {
            // Now, replace all normal anonymous types and tuples with 'a, 'b, etc. and create a
            // Structural Types: section to display their info.

            var directStructuralTypes =
                from parts in _groupMap.Values
                from part in parts
                where part.Symbol.IsAnonymousType() || part.Symbol.IsTupleType()
                select (INamedTypeSymbol)part.Symbol!;

            // If the first symbol is an anonymous delegate, just show it's full sig in-line in the main
            // description.  Otherwise, replace it with 'a, 'b etc. and show its sig in the 'Types:' section.

            if (firstSymbol.IsAnonymousDelegateType())
                directStructuralTypes = directStructuralTypes.Except([(INamedTypeSymbol)firstSymbol]);

            var info = LanguageServices.GetRequiredService<IStructuralTypeDisplayService>().GetTypeDisplayInfo(
                firstSymbol, directStructuralTypes.ToImmutableArrayOrEmpty(), _semanticModel, _position);

            if (info.TypesParts.Count > 0)
                AddToGroup(SymbolDescriptionGroups.StructuralTypes, info.TypesParts);

            foreach (var (group, parts) in _groupMap.ToArray())
                _groupMap[group] = info.ReplaceStructuralTypes(parts, _semanticModel, _position);
        }
    }
}
