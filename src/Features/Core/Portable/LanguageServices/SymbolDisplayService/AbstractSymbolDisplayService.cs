// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            AnonymousTypeDisplayService = anonymousTypeDisplayService;
        }

        public abstract ImmutableArray<SymbolDisplayPart> ToDisplayParts(ISymbol symbol, SymbolDisplayFormat format = null);
        public abstract ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, ISymbol symbol, SymbolDisplayFormat format);
        protected abstract AbstractSymbolDescriptionBuilder CreateDescriptionBuilder(Workspace workspace, SemanticModel semanticModel, int position, CancellationToken cancellationToken);

        public string ToDisplayString(ISymbol symbol, SymbolDisplayFormat format = null)
        {
            return ToDisplayParts(symbol, format).ToDisplayString();
        }

        public string ToMinimalDisplayString(SemanticModel semanticModel, int position, ISymbol symbol, SymbolDisplayFormat format = null)
        {
            return ToMinimalDisplayParts(semanticModel, position, symbol, format).ToDisplayString();
        }

        public Task<string> ToDescriptionStringAsync(Workspace workspace, SemanticModel semanticModel, int position, ISymbol symbol, SymbolDescriptionGroups groups, CancellationToken cancellationToken)
        {
            return ToDescriptionStringAsync(workspace, semanticModel, position, ImmutableArray.Create<ISymbol>(symbol), groups, cancellationToken);
        }

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
