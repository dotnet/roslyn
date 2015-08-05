// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractKeywordCompletionProvider<TContext> : AbstractCompletionProvider
    {
        private readonly ImmutableArray<IKeywordRecommender<TContext>> _keywordRecommenders;

        protected AbstractKeywordCompletionProvider(
            IEnumerable<IKeywordRecommender<TContext>> keywordRecommenders)
        {
            _keywordRecommenders = ImmutableArray.CreateRange(keywordRecommenders);
        }

        protected abstract Task<TContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken);
        protected abstract TextSpan GetTextChangeSpan(SourceText text, int position);

        private class Comparer : IEqualityComparer<RecommendedKeyword>
        {
            public bool Equals(RecommendedKeyword x, RecommendedKeyword y)
            {
                return x.Glyph == y.Glyph && x.Keyword == y.Keyword;
            }

            public int GetHashCode(RecommendedKeyword obj)
            {
                return Hash.Combine(obj.Keyword.GetHashCode(), obj.Glyph.GetHashCode());
            }
        }

        private static readonly Comparer s_comparer = new Comparer();

        protected override async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(
            Document document, int position, CompletionTrigger trigger,
            CancellationToken cancellationToken)
        {
            var options = document.Project.Solution.Workspace.Options;

            if (!options.GetOption(CompletionOptions.IncludeKeywords, document.Project.Language))
            {
                return null;
            }

            using (Logger.LogBlock(FunctionId.Completion_KeywordCompletionProvider_GetItemsWorker, cancellationToken))
            {
                var unionKeywords = await document.GetUnionResultsFromDocumentAndLinks(s_comparer, async (doc, ct) => await RecommendKeywordsAsync(doc, position, ct).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var textChangeSpan = this.GetTextChangeSpan(text, position);
                var completions = unionKeywords.Select(s => CreateItem(textChangeSpan, s)).ToList();

                return completions;
            }
        }

        protected virtual CompletionItem CreateItem(TextSpan span, RecommendedKeyword keyword)
        {
            return new KeywordCompletionItem(
                this,
                displayText: keyword.Keyword,
                filterSpan: span,
                descriptionFactory: (c) => Task.FromResult(keyword.DescriptionFactory(c)),
                glyph: Glyph.Keyword,
                isIntrinsic: keyword.IsIntrinsic);
        }

        protected virtual async Task<IEnumerable<RecommendedKeyword>> RecommendKeywordsAsync(
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken))
            {
                return null;
            }

            var context = await CreateContextAsync(document, position, cancellationToken).ConfigureAwait(false);

            var set = new HashSet<RecommendedKeyword>();
            foreach (var recommender in _keywordRecommenders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var keywords = recommender.RecommendKeywords(position, context, cancellationToken);
                if (keywords != null)
                {
                    set.AddRange(keywords);
                }
            }

            return set;
        }
    }
}
