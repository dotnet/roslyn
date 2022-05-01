// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal abstract class AbstractRemoveUnnecessaryImportsService<T> :
        IRemoveUnnecessaryImportsService,
        IEqualityComparer<T> where T : SyntaxNode
    {
        protected abstract IUnnecessaryImportsProvider UnnecessaryImportsProvider { get; }

        public Task<Document> RemoveUnnecessaryImportsAsync(Document document, SyntaxFormattingOptions? formattingOptions, CancellationToken cancellationToken)
            => RemoveUnnecessaryImportsAsync(document, predicate: null, formattingOptions, cancellationToken);

        public abstract Task<Document> RemoveUnnecessaryImportsAsync(Document fromDocument, Func<SyntaxNode, bool>? predicate, SyntaxFormattingOptions? formattingOptions, CancellationToken cancellationToken);

        protected static SyntaxToken StripNewLines(Document document, SyntaxToken token)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var trimmedLeadingTrivia = token.LeadingTrivia.SkipWhile(syntaxFacts.IsEndOfLineTrivia).ToList();

            // If the list ends with 3 newlines remove the last one until there's only 2 newlines to end the leading trivia.
            while (trimmedLeadingTrivia.Count >= 3 &&
                   syntaxFacts.IsEndOfLineTrivia(trimmedLeadingTrivia[^3]) &&
                   syntaxFacts.IsEndOfLineTrivia(trimmedLeadingTrivia[^2]) &&
                   syntaxFacts.IsEndOfLineTrivia(trimmedLeadingTrivia[^1]))
            {
                trimmedLeadingTrivia.RemoveAt(trimmedLeadingTrivia.Count - 1);
            }

            return token.WithLeadingTrivia(trimmedLeadingTrivia);
        }

        protected async Task<HashSet<T>> GetCommonUnnecessaryImportsOfAllContextAsync(
            Document document, Func<SyntaxNode, bool> predicate, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var unnecessaryImports = new HashSet<T>(this);
            unnecessaryImports.AddRange(UnnecessaryImportsProvider.GetUnnecessaryImports(
                model, root, predicate, cancellationToken).Cast<T>());
            foreach (var current in document.GetLinkedDocuments())
            {
                var currentModel = await current.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var currentRoot = await current.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                unnecessaryImports.IntersectWith(UnnecessaryImportsProvider.GetUnnecessaryImports(
                    currentModel, currentRoot, predicate, cancellationToken).Cast<T>());
            }

            return unnecessaryImports;
        }

        bool IEqualityComparer<T>.Equals(T? x, T? y)
            => x?.Span == y?.Span;

        int IEqualityComparer<T>.GetHashCode(T obj)
            => obj.Span.GetHashCode();
    }
}
