// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal interface ISymbolDeclarationService : ILanguageService
    {
        /// <summary>
        /// Given a symbol in source, returns the syntax nodes that compromise its declarations.
        /// This differs from symbol.Locations in that Locations returns a list of ILocations that
        /// normally correspond to the name node of the symbol.
        /// </summary>
        ImmutableArray<SyntaxReference> GetDeclarations(ISymbol symbol);
    }
}
