// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
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
                if (current.DeclaringSyntaxReferences.Length == 0)
                    continue;

                if (current is IPropertySymbol)
                    return current;

                // If this is an initializer for a property's backing field, then we want to 
                // search for results within the property itself.
                if (current is IFieldSymbol field)
                {
                    return field is { IsImplicitlyDeclared: true, AssociatedSymbol.Kind: SymbolKind.Property }
                        ? field.AssociatedSymbol
                        : field;
                }

                // Note: this may hit a containing local-function/lambda.  That's fine as that's still the scope we want
                // to look for this local within.
                if (current is IMethodSymbol)
                    return current;
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
            using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var tokens);

            foreach (var declaration in service.GetDeclarations(container))
            {
                var syntax = declaration.GetSyntax(cancellationToken);
                if (syntax.SyntaxTree != state.SyntaxTree)
                    continue;

                foreach (var token in syntax.DescendantTokens())
                {
                    if (TokensMatch(state, token, symbol.Name))
                        tokens.Add(token);
                }
            }

            return FindReferencesInTokensAsync(symbol, state, tokens.ToImmutable(), cancellationToken);
        }
    }
}
