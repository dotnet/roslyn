// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal abstract class AbstractRemoveUnnecessaryImportsService<T> :
        IRemoveUnnecessaryImportsService,
        IUnnecessaryImportsService,
        IEqualityComparer<T> where T : SyntaxNode
    {
        public Task<Document> RemoveUnnecessaryImportsAsync(Document document, CancellationToken cancellationToken)
            => RemoveUnnecessaryImportsAsync(document, predicate: null, cancellationToken: cancellationToken);

        public abstract Task<Document> RemoveUnnecessaryImportsAsync(Document fromDocument, Func<SyntaxNode, bool> predicate, CancellationToken cancellationToken);

        public ImmutableArray<SyntaxNode> GetUnnecessaryImports(
            SemanticModel model, CancellationToken cancellationToken)
        {
            var root = model.SyntaxTree.GetRoot(cancellationToken);
            return GetUnnecessaryImports(model, root, predicate: null, cancellationToken: cancellationToken).CastArray<SyntaxNode>();
        }

        protected SyntaxToken StripNewLines(Document document, SyntaxToken token)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var trimmedLeadingTrivia = token.LeadingTrivia.SkipWhile(t => syntaxFacts.IsEndOfLineTrivia(t)).ToList();

            // If the list ends with 3 newlines remove the last one until there's only 2 newlines to end the leading trivia.
            while (trimmedLeadingTrivia.Count >= 3 &&
                   syntaxFacts.IsEndOfLineTrivia(trimmedLeadingTrivia[trimmedLeadingTrivia.Count - 3]) &&
                   syntaxFacts.IsEndOfLineTrivia(trimmedLeadingTrivia[trimmedLeadingTrivia.Count - 2]) &&
                   syntaxFacts.IsEndOfLineTrivia(trimmedLeadingTrivia[trimmedLeadingTrivia.Count - 1]))
            {
                trimmedLeadingTrivia.RemoveAt(trimmedLeadingTrivia.Count - 1);
            }

            return token.WithLeadingTrivia(trimmedLeadingTrivia);
        }

        protected abstract ImmutableArray<T> GetUnnecessaryImports(
            SemanticModel model, SyntaxNode root,
            Func<SyntaxNode, bool> predicate, CancellationToken cancellationToken);

        protected async Task<HashSet<T>> GetCommonUnnecessaryImportsOfAllContextAsync(
            Document document, Func<SyntaxNode, bool> predicate, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var unnecessaryImports = new HashSet<T>(this);
            unnecessaryImports.AddRange(GetUnnecessaryImports(
                model, root, predicate, cancellationToken));
            foreach (var current in document.GetLinkedDocuments())
            {
                var currentModel = await current.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var currentRoot = await current.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                unnecessaryImports.IntersectWith(GetUnnecessaryImports(
                    currentModel, currentRoot, predicate, cancellationToken));
            }

            return unnecessaryImports;
        }

        bool IEqualityComparer<T>.Equals(T x, T y)
        {
            return x.Span == y.Span;
        }

        int IEqualityComparer<T>.GetHashCode(T obj)
        {
            return obj.Span.GetHashCode();
        }
    }
}
