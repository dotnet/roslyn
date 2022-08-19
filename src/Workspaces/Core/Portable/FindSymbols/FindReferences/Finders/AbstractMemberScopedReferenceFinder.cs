// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal abstract class AbstractMemberScopedReferenceFinder<TSymbol> : AbstractReferenceFinder<TSymbol>
        where TSymbol : ISymbol
    {
        protected abstract bool TokensMatch(
            FindReferencesDocumentState state, SyntaxToken token, string name);

        protected sealed override bool CanFind(TSymbol symbol)
            => true;

        protected sealed override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            TSymbol symbol,
            HashSet<string>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var location = symbol.Locations.FirstOrDefault();
            if (location == null || !location.IsInSource)
                return SpecializedTasks.EmptyImmutableArray<Document>();

            var document = project.GetDocument(location.SourceTree);
            if (document == null)
                return SpecializedTasks.EmptyImmutableArray<Document>();

            if (documents != null && !documents.Contains(document))
                return SpecializedTasks.EmptyImmutableArray<Document>();

            return Task.FromResult(ImmutableArray.Create(document));
        }

        protected sealed override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            TSymbol symbol,
            FindReferencesDocumentState state,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var container = GetContainer(symbol);
            if (container != null)
                return await FindReferencesInContainerAsync(symbol, container, state, cancellationToken).ConfigureAwait(false);

            if (symbol.ContainingType != null && symbol.ContainingType.IsScriptClass)
            {
                var tokens = await FindMatchingIdentifierTokensAsync(state, symbol.Name, cancellationToken).ConfigureAwait(false);
                return await FindReferencesInTokensAsync(symbol, state, tokens, cancellationToken).ConfigureAwait(false);
            }

            return ImmutableArray<FinderLocation>.Empty;
        }

        private static ISymbol? GetContainer(ISymbol symbol)
        {
            for (var current = symbol; current != null; current = current.ContainingSymbol)
            {
                if (current is IPropertySymbol)
                    return current;

                // If this is an initializer for a property's backing field, then we want to 
                // search for results within the property itself.
                if (current is IFieldSymbol field)
                {
                    if (field.IsImplicitlyDeclared &&
                        field.AssociatedSymbol?.Kind == SymbolKind.Property)
                    {
                        return field.AssociatedSymbol;
                    }
                    else
                    {
                        return field;
                    }
                }

                if (current is IMethodSymbol { MethodKind: not MethodKind.AnonymousFunction and not MethodKind.LocalFunction } method)
                    return method;
            }

            return null;
        }

        private ValueTask<ImmutableArray<FinderLocation>> FindReferencesInContainerAsync(
            TSymbol symbol,
            ISymbol container,
            FindReferencesDocumentState state,
            CancellationToken cancellationToken)
        {
            var service = state.Document.GetRequiredLanguageService<ISymbolDeclarationService>();
            var tokens = service.GetDeclarations(container)
                .SelectMany(r => r.GetSyntax(cancellationToken)
                    .DescendantTokens()
                    .Where(t => TokensMatch(state, t, symbol.Name)))
                .ToImmutableArray();

            return FindReferencesInTokensAsync(symbol, state, tokens, cancellationToken);
        }
    }
}
