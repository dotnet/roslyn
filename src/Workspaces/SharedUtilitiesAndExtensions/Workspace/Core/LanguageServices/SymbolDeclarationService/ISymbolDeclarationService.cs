// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageService
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
