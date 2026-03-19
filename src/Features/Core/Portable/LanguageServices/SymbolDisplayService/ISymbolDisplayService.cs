// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageService;

internal interface ISymbolDisplayService : ILanguageService
{
    Task<ImmutableArray<SymbolDisplayPart>> ToDescriptionPartsAsync(SemanticModel semanticModel, int position, ImmutableArray<ISymbol> symbols, SymbolDescriptionOptions options, SymbolDescriptionGroups groups = SymbolDescriptionGroups.All, CancellationToken cancellationToken = default);
    Task<IDictionary<SymbolDescriptionGroups, ImmutableArray<TaggedText>>> ToDescriptionGroupsAsync(SemanticModel semanticModel, int position, ImmutableArray<ISymbol> symbols, SymbolDescriptionOptions options, CancellationToken cancellationToken = default);
}

internal static class ISymbolDisplayServiceExtensions
{
    extension(ISymbolDisplayService service)
    {
        public Task<string> ToDescriptionStringAsync(SemanticModel semanticModel, int position, ISymbol symbol, SymbolDescriptionOptions options, SymbolDescriptionGroups groups = SymbolDescriptionGroups.All, CancellationToken cancellationToken = default)
            => service.ToDescriptionStringAsync(semanticModel, position, [symbol], options, groups, cancellationToken);

        public async Task<string> ToDescriptionStringAsync(SemanticModel semanticModel, int position, ImmutableArray<ISymbol> symbols, SymbolDescriptionOptions options, SymbolDescriptionGroups groups = SymbolDescriptionGroups.All, CancellationToken cancellationToken = default)
        {
            var parts = await service.ToDescriptionPartsAsync(semanticModel, position, symbols, options, groups, cancellationToken).ConfigureAwait(false);
            return parts.ToDisplayString();
        }
    }
}
