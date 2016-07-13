﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;

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

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var options = context.Options;
            var cancellationToken = context.CancellationToken;

            using (Logger.LogBlock(FunctionId.Completion_KeywordCompletionProvider_GetItemsWorker, cancellationToken))
            {
                var keywords = await document.GetUnionItemsFromDocumentAndLinkedDocumentsAsync(
                    s_comparer,
                    (doc, ct) => RecommendKeywordsAsync(doc, position, ct),
                    cancellationToken).ConfigureAwait(false);

                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                foreach (var keyword in keywords)
                {
                    context.AddItem(CreateItem(keyword, context.DefaultItemSpan));
                }
            }
        }

        private static ImmutableArray<string> s_Tags = ImmutableArray.Create(CompletionTags.Intrinsic);

        protected virtual CompletionItem CreateItem(RecommendedKeyword keyword, TextSpan span)
        {
            return CommonCompletionItem.Create(
                displayText: keyword.Keyword,
                span: span,
                description: keyword.DescriptionFactory(CancellationToken.None),
                glyph: Glyph.Keyword,
                tags: s_Tags,
                matchPriority: keyword.MatchPriority);
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

                var keywords = await recommender.RecommendKeywordsAsync(position, context, cancellationToken).ConfigureAwait(false);
                if (keywords != null)
                {
                    set.AddRange(keywords);
                }
            }

            return set;
        }

        public override async Task<TextChange?> GetTextChangeAsync(Document document, CompletionItem item, char? ch, CancellationToken cancellationToken)
        {
            var insertionText = item.DisplayText;
            if (ch == ' ')
            {
                var currentSnapshot = document.Project.Solution.Workspace.CurrentSolution.GetDocument(document.Id);
                var text = await currentSnapshot.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var textTypedSoFar = text.GetSubText(GetCurrentSpan(item.Span, text)).ToString() + ch;

                if (textTypedSoFar.Length > 0 && insertionText.StartsWith(textTypedSoFar, StringComparison.OrdinalIgnoreCase))
                {
                    insertionText = insertionText.Substring(0, textTypedSoFar.Length - 1);
                }
            }

            return new TextChange(item.Span, insertionText);
        }

        internal abstract TextSpan GetCurrentSpan(TextSpan span, SourceText text);
    }
}
