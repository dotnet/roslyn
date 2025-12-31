// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageService;

internal abstract partial class AbstractSymbolDisplayService(LanguageServices services) : ISymbolDisplayService
{
    protected readonly LanguageServices LanguageServices = services;

    protected abstract AbstractSymbolDescriptionBuilder CreateDescriptionBuilder(SemanticModel semanticModel, int position, SymbolDescriptionOptions options, CancellationToken cancellationToken);

    public async Task<ImmutableArray<SymbolDisplayPart>> ToDescriptionPartsAsync(SemanticModel semanticModel, int position, ImmutableArray<ISymbol> symbols, SymbolDescriptionOptions options, SymbolDescriptionGroups groups, CancellationToken cancellationToken)
    {
        if (symbols.Length == 0)
            return [];

        var builder = CreateDescriptionBuilder(semanticModel, position, options, cancellationToken);
        return await builder.BuildDescriptionAsync(symbols, groups).ConfigureAwait(false);
    }

    public async Task<IDictionary<SymbolDescriptionGroups, ImmutableArray<TaggedText>>> ToDescriptionGroupsAsync(
        SemanticModel semanticModel, int position, ImmutableArray<ISymbol> symbols, SymbolDescriptionOptions options, CancellationToken cancellationToken)
    {
        if (symbols.Length == 0)
            return SpecializedCollections.EmptyDictionary<SymbolDescriptionGroups, ImmutableArray<TaggedText>>();

        var builder = CreateDescriptionBuilder(semanticModel, position, options, cancellationToken);
        return await builder.BuildDescriptionSectionsAsync(symbols).ConfigureAwait(false);
    }
}
