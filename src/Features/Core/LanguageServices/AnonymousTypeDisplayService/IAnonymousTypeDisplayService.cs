// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal interface IAnonymousTypeDisplayService : ILanguageService
    {
        AnonymousTypeDisplayInfo GetNormalAnonymousTypeDisplayInfo(
            ISymbol orderSymbol,
            IEnumerable<INamedTypeSymbol> directNormalAnonymousTypeReferences,
            SemanticModel semanticModel,
            int position,
            ISymbolDisplayService displayService);

        IEnumerable<SymbolDisplayPart> GetAnonymousTypeParts(
            INamedTypeSymbol anonymousType,
            SemanticModel semanticModel,
            int position,
            ISymbolDisplayService displayService);
    }
}
