// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private abstract class Reference : IComparable<Reference>, IEquatable<Reference>
        {
            protected readonly AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider;
            public readonly SearchResult SearchResult;

            protected Reference(AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider, SearchResult searchResult)
            {
                this.provider = provider;
                this.SearchResult = searchResult;
            }

            public virtual int CompareTo(Reference other)
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

                // If the weight are the same, just order them based on their names.
                return INamespaceOrTypeSymbolExtensions.CompareNameParts(
                    this.SearchResult.NameParts, other.SearchResult.NameParts);
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

            protected void ReplaceNameNode(
                ref SyntaxNode contextNode, ref Document document, CancellationToken cancellationToken)
            {
                if (!this.SearchResult.ShouldRenameNode())
                {
                    return;
                }

                var identifier = SearchResult.NameNode.GetFirstToken();
                var generator = SyntaxGenerator.GetGenerator(document);
                var newIdentifier = generator.IdentifierName(SearchResult.DesiredName).GetFirstToken().WithTriviaFrom(identifier);
                var annotation = new SyntaxAnnotation();

                var root = contextNode.SyntaxTree.GetRoot(cancellationToken);
                root = root.ReplaceToken(identifier, newIdentifier.WithAdditionalAnnotations(annotation));
                document = document.WithSyntaxRoot(root);
                contextNode = root.GetAnnotatedTokens(annotation).First().Parent;
            }

            public abstract Task<CodeAction> CreateCodeActionAsync(Document document, SyntaxNode node, bool placeSystemNamespaceFirst, CancellationToken cancellationToken);
        }
    }
}
