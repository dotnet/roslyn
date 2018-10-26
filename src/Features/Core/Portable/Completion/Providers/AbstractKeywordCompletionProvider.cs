// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractKeywordCompletionProvider<TContext> : CommonCompletionProvider
    {
        private readonly ImmutableArray<IKeywordRecommender<TContext>> _keywordRecommenders;

        protected AbstractKeywordCompletionProvider(
            ImmutableArray<IKeywordRecommender<TContext>> keywordRecommenders)
        {
            _keywordRecommenders = keywordRecommenders;
        }

        protected abstract Task<TContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken);

        private class Comparer : IEqualityComparer<CompletionItem>
        {
            public bool Equals(CompletionItem x, CompletionItem y)
            {
                return x.DisplayText == y.DisplayText;
            }

            public int GetHashCode(CompletionItem obj)
            {
                return Hash.Combine(obj.DisplayText.GetHashCode(), obj.DisplayText.GetHashCode());
            }
        }

        private static readonly Comparer s_comparer = new Comparer();

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var options = context.Options;
            var cancellationToken = context.CancellationToken;

            using (Logger.LogBlock(FunctionId.Completion_KeywordCompletionProvider_GetItemsWorker, cancellationToken))
            {
                var completionItems = await document.GetUnionItemsFromDocumentAndLinkedDocumentsAsync(
                    s_comparer,
                    (doc, ct) => RecommendCompletionItemsAsync(doc, position, ct),
                    cancellationToken).ConfigureAwait(false);

                foreach (var completionItem in completionItems)
                {
                    context.AddItem(completionItem);
                }
            }
        }

        private async Task<IEnumerable<CompletionItem>> RecommendCompletionItemsAsync(Document doc, int position, CancellationToken ct)
        {
            var syntaxContext = await CreateContextAsync(doc, position, ct).ConfigureAwait(false);
            var keywords = await RecommendKeywordsAsync(doc, position, syntaxContext, ct).ConfigureAwait(false);
            return keywords?.Select(k => CreateItem(k, syntaxContext));
        }

        protected static ImmutableArray<string> s_Tags = ImmutableArray.Create(WellKnownTags.Intrinsic);

        protected static CompletionItemRules s_keywordRules = CompletionItemRules.Default;

        protected virtual CompletionItem CreateItem(RecommendedKeyword keyword, TContext context)
        {
            return CommonCompletionItem.Create(
                displayText: keyword.Keyword,
                displayTextSuffix: "",
                rules: s_keywordRules.WithMatchPriority(keyword.MatchPriority),
                description: keyword.DescriptionFactory(CancellationToken.None),
                glyph: Glyph.Keyword,
                tags: s_Tags);
        }

        protected virtual async Task<IEnumerable<RecommendedKeyword>> RecommendKeywordsAsync(
            Document document,
            int position,
            TContext context,
            CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken))
            {
                return null;
            }

            var set = new HashSet<RecommendedKeyword>();
            foreach (var recommender in _keywordRecommenders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var keywords = await recommender.RecommendKeywordsAsync(position, context, cancellationToken).ConfigureAwait(false);
                if (keywords != null)
                {
                    set.AddRange(keywords);
                }
            }

            return set;
        }

        public override Task<TextChange?> GetTextChangeAsync(Document document, CompletionItem item, char? ch, CancellationToken cancellationToken)
        {
            return Task.FromResult((TextChange?)new TextChange(item.Span, item.DisplayText));
        }

        internal abstract TextSpan GetCurrentSpan(TextSpan span, SourceText text);
    }
}
