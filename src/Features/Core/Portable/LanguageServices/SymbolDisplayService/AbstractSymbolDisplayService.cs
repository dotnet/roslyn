// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract partial class AbstractSymbolDisplayService : ISymbolDisplayService
    {
        protected readonly IAnonymousTypeDisplayService AnonymousTypeDisplayService;

        protected AbstractSymbolDisplayService(IAnonymousTypeDisplayService anonymousTypeDisplayService)
            => AnonymousTypeDisplayService = anonymousTypeDisplayService;

        protected abstract AbstractSymbolDescriptionBuilder CreateDescriptionBuilder(Workspace workspace, SemanticModel semanticModel, int position, CancellationToken cancellationToken);

        public Task<string> ToDescriptionStringAsync(Workspace workspace, SemanticModel semanticModel, int position, ISymbol symbol, SymbolDescriptionGroups groups, CancellationToken cancellationToken)
            => ToDescriptionStringAsync(workspace, semanticModel, position, ImmutableArray.Create<ISymbol>(symbol), groups, cancellationToken);

        public async Task<string> ToDescriptionStringAsync(Workspace workspace, SemanticModel semanticModel, int position, ImmutableArray<ISymbol> symbols, SymbolDescriptionGroups groups, CancellationToken cancellationToken)
        {
            var parts = await ToDescriptionPartsAsync(workspace, semanticModel, position, symbols, groups, cancellationToken).ConfigureAwait(false);
            return parts.ToDisplayString();
        }

        public async Task<ImmutableArray<SymbolDisplayPart>> ToDescriptionPartsAsync(Workspace workspace, SemanticModel semanticModel, int position, ImmutableArray<ISymbol> symbols, SymbolDescriptionGroups groups, CancellationToken cancellationToken)
        {
            if (symbols.Length == 0)
            {
                return ImmutableArray.Create<SymbolDisplayPart>();
            }

            var builder = CreateDescriptionBuilder(workspace, semanticModel, position, cancellationToken);
            return await builder.BuildDescriptionAsync(symbols, groups).ConfigureAwait(false);
        }

        public async Task<IDictionary<SymbolDescriptionGroups, ImmutableArray<TaggedText>>> ToDescriptionGroupsAsync(
            Workspace workspace, SemanticModel semanticModel, int position, ImmutableArray<ISymbol> symbols, CancellationToken cancellationToken)
        {
            if (symbols.Length == 0)
            {
                return SpecializedCollections.EmptyDictionary<SymbolDescriptionGroups, ImmutableArray<TaggedText>>();
            }

            var builder = CreateDescriptionBuilder(workspace, semanticModel, position, cancellationToken);
            return await builder.BuildDescriptionSectionsAsync(symbols).ConfigureAwait(false);
        }
    }
}
