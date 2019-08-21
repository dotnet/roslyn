// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        private abstract class Reference : IEquatable<Reference>
        {
            protected readonly AbstractAddImportFeatureService<TSimpleNameSyntax> provider;
            public readonly SearchResult SearchResult;

            protected Reference(
                AbstractAddImportFeatureService<TSimpleNameSyntax> provider,
                SearchResult searchResult)
            {
                this.provider = provider;
                SearchResult = searchResult;
            }

            public int CompareTo(Document document, Reference other)
                => IComparableHelper.CompareTo(this, other, r => GetComparisonComponents(r, document));

            private static IEnumerable<IComparable> GetComparisonComponents(Reference reference, Document document)
            {
                var searchResult = reference.SearchResult;
                // If references have different weights, order by the ones with lower weight (i.e.
                // they are better matches).
                yield return searchResult.Weight;

                // Prefer the name doesn't need to change.
                yield return !searchResult.DesiredNameMatchesSourceName(document);

                // Sort by the name we're  changing to.
                yield return searchResult.DesiredName;

                // If the weights are the same and no names changed, just order 
                // them based on the namespace we're adding an import for.
                foreach (var c in INamespaceOrTypeSymbolExtensions.GetComparisonComponents(searchResult.NameParts, placeSystemNamespaceFirst: true))
                {
                    yield return c;
                }
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Reference);
            }

            public bool Equals(Reference other)
            {
                return other != null &&
                    other.SearchResult.NameParts != null &&
                    SearchResult.NameParts.SequenceEqual(other.SearchResult.NameParts);
            }

            public override int GetHashCode()
            {
                return Hash.CombineValues(SearchResult.NameParts);
            }

            protected async Task<(SyntaxNode, Document)> ReplaceNameNodeAsync(
                SyntaxNode contextNode, Document document, CancellationToken cancellationToken)
            {
                if (!SearchResult.DesiredNameDiffersFromSourceName())
                {
                    return (contextNode, document);
                }

                var identifier = SearchResult.NameNode.GetFirstToken();
                var generator = SyntaxGenerator.GetGenerator(document);
                var newIdentifier = generator.IdentifierName(SearchResult.DesiredName).GetFirstToken().WithTriviaFrom(identifier);
                var annotation = new SyntaxAnnotation();

                var root = contextNode.SyntaxTree.GetRoot(cancellationToken);
                root = root.ReplaceToken(identifier, newIdentifier.WithAdditionalAnnotations(annotation));

                var newDocument = document.WithSyntaxRoot(root);
                var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newContextNode = newRoot.GetAnnotatedTokens(annotation).First().Parent;

                return (newContextNode, newDocument);
            }

            public abstract Task<AddImportFixData> TryGetFixDataAsync(
                Document document, SyntaxNode node, bool placeSystemNamespaceFirst, CancellationToken cancellationToken);

            protected async Task<ImmutableArray<TextChange>> GetTextChangesAsync(
                Document document, SyntaxNode node, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
            {
                var originalDocument = document;

                (node, document) = await ReplaceNameNodeAsync(
                    node, document, cancellationToken).ConfigureAwait(false);

                var newDocument = await provider.AddImportAsync(
                    node, SearchResult.NameParts, document, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);

                var cleanedDocument = await CodeAction.CleanupDocumentAsync(
                    newDocument, cancellationToken).ConfigureAwait(false);

                var textChanges = await cleanedDocument.GetTextChangesAsync(
                    originalDocument, cancellationToken).ConfigureAwait(false);

                return textChanges.ToImmutableArray();
            }
        }
    }
}
