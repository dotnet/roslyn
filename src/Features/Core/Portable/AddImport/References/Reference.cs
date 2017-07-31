﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
                this.SearchResult = searchResult;
            }

            public int CompareTo(Document document, Reference other)
            {
                // If references have different weights, order by the ones with lower weight (i.e.
                // they are better matches).
                if (this.SearchResult.Weight < other.SearchResult.Weight)
                {
                    return -1;
                }

                if (this.SearchResult.Weight > other.SearchResult.Weight)
                {
                    return 1;
                }

                if (this.SearchResult.DesiredNameMatchesSourceName(document))
                {
                    if (!other.SearchResult.DesiredNameMatchesSourceName(document))
                    {
                        // Prefer us as our name doesn't need to change.
                        return -1;
                    }
                }
                else
                {
                    if (other.SearchResult.DesiredNameMatchesSourceName(document))
                    {
                        // Prefer them as their name doesn't need to change.
                        return 1;
                    }
                    else
                    {
                        // Both our names need to change.  Sort by the name we're 
                        // changing to.
                        var diff = StringComparer.OrdinalIgnoreCase.Compare(
                            this.SearchResult.DesiredName, other.SearchResult.DesiredName);
                        if (diff != 0)
                        {
                            return diff;
                        }
                    }
                }

                // If the weights are the same and no names changed, just order 
                // them based on the namespace we're adding an import for.
                return INamespaceOrTypeSymbolExtensions.CompareNameParts(
                    this.SearchResult.NameParts, other.SearchResult.NameParts,
                    placeSystemNamespaceFirst: true);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Reference);
            }

            public bool Equals(Reference other)
            {
                return other != null &&
                    other.SearchResult.NameParts != null &&
                    this.SearchResult.NameParts.SequenceEqual(other.SearchResult.NameParts);
            }

            public override int GetHashCode()
            {
                return Hash.CombineValues(this.SearchResult.NameParts);
            }

            protected async Task<(SyntaxNode, Document)> ReplaceNameNodeAsync(
                SyntaxNode contextNode, Document document, CancellationToken cancellationToken)
            {
                if (!this.SearchResult.DesiredNameDiffersFromSourceName())
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

                (node, document) = await this.ReplaceNameNodeAsync(
                    node, document, cancellationToken).ConfigureAwait(false);

                var newDocument = await this.provider.AddImportAsync(
                    node, this.SearchResult.NameParts, document, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);

                var cleanedDocument = await CodeAction.CleanupDocumentAsync(
                    newDocument, cancellationToken).ConfigureAwait(false);

                var textChanges = await cleanedDocument.GetTextChangesAsync(
                    originalDocument, cancellationToken).ConfigureAwait(false);

                return textChanges.ToImmutableArray();
            }
        }
    }
}
