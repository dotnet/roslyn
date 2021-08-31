// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal interface IStructuralTypeDisplayService : ILanguageService
    {
        StructuralTypeDisplayInfo GetTypeDisplayInfo(
            ISymbol orderSymbol,
            IEnumerable<INamedTypeSymbol> directNormalAnonymousTypeReferences,
            SemanticModel semanticModel,
            int position);

        ImmutableArray<SymbolDisplayPart> GetTypeParts(
            INamedTypeSymbol anonymousType,
            SemanticModel semanticModel,
            int position);
    }
}
