﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal sealed class PropertyAccessorSymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IMethodSymbol>
    {
        protected override bool CanFind(IMethodSymbol symbol)
            => symbol.MethodKind.IsPropertyAccessor();

        protected override ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
            IMethodSymbol symbol,
            Solution solution,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // If we've been asked to search for specific accessors, then do not cascade.
            // We don't want to produce results for the associated property.
            return options.AssociatePropertyReferencesWithSpecificAccessor || symbol.AssociatedSymbol == null
                ? new(ImmutableArray<ISymbol>.Empty)
                : new(ImmutableArray.Create(symbol.AssociatedSymbol));
        }

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol symbol,
            HashSet<string>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // First, find any documents with the full name of the accessor (i.e. get_Goo).
            // This will find explicit calls to the method (which can happen when C# references
            // a VB parameterized property).
            var documentsWithName = await FindDocumentsAsync(project, documents, cancellationToken, symbol.Name).ConfigureAwait(false);

            var propertyDocuments = ImmutableArray<Document>.Empty;
            if (symbol.AssociatedSymbol is IPropertySymbol property &&
                options.AssociatePropertyReferencesWithSpecificAccessor)
            {
                // we want to associate normal property references with the specific accessor being
                // referenced.  So we also need to include documents with our property's name. Just
                // defer to the Property finder to find these docs and combine them with the result.
                propertyDocuments = await ReferenceFinders.Property.DetermineDocumentsToSearchAsync(
                    property, globalAliases, project, documents,
                    options with { AssociatePropertyReferencesWithSpecificAccessor = false },
                    cancellationToken).ConfigureAwait(false);
            }

            var documentsWithGlobalAttributes = await FindDocumentsWithGlobalSuppressMessageAttributeAsync(project, documents, cancellationToken).ConfigureAwait(false);
            return documentsWithName.Concat(propertyDocuments, documentsWithGlobalAttributes);
        }

        protected override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol symbol,
            FindReferencesDocumentState state,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var references = await FindReferencesInDocumentUsingSymbolNameAsync(
                symbol, state, cancellationToken).ConfigureAwait(false);

            if (symbol.AssociatedSymbol is not IPropertySymbol property ||
                !options.AssociatePropertyReferencesWithSpecificAccessor)
            {
                return references;
            }

            var propertyReferences = await ReferenceFinders.Property.FindReferencesInDocumentAsync(
                property, state,
                options with { AssociatePropertyReferencesWithSpecificAccessor = false },
                cancellationToken).ConfigureAwait(false);

            var accessorReferences = propertyReferences.WhereAsArray(
                loc =>
                {
                    var accessors = GetReferencedAccessorSymbols(
                        state, property, loc.Node, cancellationToken);
                    return accessors.Contains(symbol);
                });

            return references.Concat(accessorReferences);
        }
    }
}
