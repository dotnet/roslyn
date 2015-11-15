// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractKeywordCompletionProvider<TContext> : CompletionListProvider
    {
        private readonly ImmutableArray<IKeywordRecommender<TContext>> _keywordRecommenders;

        protected AbstractKeywordCompletionProvider(
            ImmutableArray<IKeywordRecommender<TContext>> keywordRecommenders)
        {
            _keywordRecommenders = keywordRecommenders;
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

        public override async Task ProduceCompletionListAsync(CompletionListContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var options = context.Options;
            var cancellationToken = context.CancellationToken;

            if (!options.GetOption(CompletionOptions.IncludeKeywords, document.Project.Language))
            {
                return;
            }

            using (Logger.LogBlock(FunctionId.Completion_KeywordCompletionProvider_GetItemsWorker, cancellationToken))
            {
                var keywords = await document.GetUnionItemsFromDocumentAndLinkedDocumentsAsync(
                    s_comparer,
                    (doc, ct) => RecommendKeywordsAsync(doc, position, ct),
                    cancellationToken).ConfigureAwait(false);

                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var filterSpan = this.GetTextChangeSpan(text, position);

                foreach (var keyword in keywords)
                {
                    context.AddItem(CreateItem(keyword, filterSpan));
                }
            }
        }

        protected virtual CompletionItem CreateItem(RecommendedKeyword keyword, TextSpan filterSpan)
        {
            return new KeywordCompletionItem(
                this,
                displayText: keyword.Keyword,
                filterSpan: filterSpan,
                descriptionFactory: c => Task.FromResult(keyword.DescriptionFactory(c)),
                glyph: Glyph.Keyword,
                isIntrinsic: keyword.IsIntrinsic,
                preselect: keyword.ShouldPreselect);
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
