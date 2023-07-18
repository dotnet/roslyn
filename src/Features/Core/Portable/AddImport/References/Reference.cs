// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
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
            {
                var diff = ComparerWithState.CompareTo(this, other, document, s_comparers);
                if (diff != 0)
                {
                    return diff;
                }

                // Both our names need to change.  Sort by the name we're 
                // changing to.
                diff = StringComparer.OrdinalIgnoreCase.Compare(
                    SearchResult.DesiredName, other.SearchResult.DesiredName);
                if (diff != 0)
                {
                    return diff;
                }

                // If the weights are the same and no names changed, just order 
                // them based on the namespace we're adding an import for.
                return INamespaceOrTypeSymbolExtensions.CompareNameParts(
                        SearchResult.NameParts, other.SearchResult.NameParts,
                        placeSystemNamespaceFirst: true);
            }

            private static readonly ImmutableArray<Func<Reference, Document, IComparable>> s_comparers
                = ImmutableArray.Create<Func<Reference, Document, IComparable>>(
                    // If references have different weights, order by the ones with lower weight (i.e.
                    // they are better matches).
                    (r, d) => r.SearchResult.Weight,
                    // Prefer the name doesn't need to change.
                    (r, d) => !r.SearchResult.DesiredNameMatchesSourceName(d));

            public override bool Equals(object obj)
                => Equals(obj as Reference);

            public bool Equals(Reference other)
            {
                return other != null &&
                    other.SearchResult.NameParts != null &&
                    SearchResult.NameParts.SequenceEqual(other.SearchResult.NameParts);
            }

            public override int GetHashCode()
                => Hash.CombineValues(SearchResult.NameParts);

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
                Document document, SyntaxNode node, CodeCleanupOptions options, CancellationToken cancellationToken);

            protected async Task<ImmutableArray<TextChange>> GetTextChangesAsync(
                Document document, SyntaxNode node, CodeCleanupOptions options, CancellationToken cancellationToken)
            {
                var originalDocument = document;

                (node, document) = await ReplaceNameNodeAsync(
                    node, document, cancellationToken).ConfigureAwait(false);

                var newDocument = await provider.AddImportAsync(
                    node, SearchResult.NameParts, document, options.AddImportOptions, cancellationToken).ConfigureAwait(false);

                var cleanedDocument = await CodeAction.CleanupDocumentAsync(
                    newDocument, options, cancellationToken).ConfigureAwait(false);

                var textChanges = await cleanedDocument.GetTextChangesAsync(
                    originalDocument, cancellationToken).ConfigureAwait(false);

                return textChanges.ToImmutableArray();
            }
        }
    }
}
