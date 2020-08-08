// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal abstract partial class AbstractSyntacticSingleKeywordRecommender : IKeywordRecommender<CSharpSyntaxContext>
    {
        private readonly bool _isValidInPreprocessorContext;

        protected internal SyntaxKind KeywordKind { get; }

        internal bool ShouldFormatOnCommit { get; }

        protected AbstractSyntacticSingleKeywordRecommender(
            SyntaxKind keywordKind,
            bool isValidInPreprocessorContext = false,
            bool shouldFormatOnCommit = false)
        {
            KeywordKind = keywordKind;
            _isValidInPreprocessorContext = isValidInPreprocessorContext;
            ShouldFormatOnCommit = shouldFormatOnCommit;
        }

        protected virtual Task<bool> IsValidContextAsync(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
            => Task.FromResult(IsValidContext(position, context, cancellationToken));

        protected virtual bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken) => false;

        public async Task<IEnumerable<RecommendedKeyword>> RecommendKeywordsAsync(
            int position,
            CSharpSyntaxContext context,
            CancellationToken cancellationToken)
        {
            var syntaxKind = await RecommendKeywordAsync(position, context, cancellationToken).ConfigureAwait(false);
            if (syntaxKind.HasValue)
            {
                return SpecializedCollections.SingletonEnumerable(
                    new RecommendedKeyword(SyntaxFacts.GetText(syntaxKind.Value),
                        shouldFormatOnCommit: ShouldFormatOnCommit,
                        matchPriority: ShouldPreselect(context, cancellationToken) ? SymbolMatchPriority.Keyword : MatchPriority.Default));
            }

            return null;
        }

        protected virtual bool ShouldPreselect(CSharpSyntaxContext context, CancellationToken cancellationToken) => false;

        private async Task<SyntaxKind?> RecommendKeywordAsync(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // NOTE: The collector ensures that we're not in "NonUserCode" like comments, strings, inactive code
            // for perf reasons.
            if (!_isValidInPreprocessorContext &&
                context.IsPreProcessorDirectiveContext)
            {
                return null;
            }

            if (!await IsValidContextAsync(position, context, cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            return KeywordKind;
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly AbstractSyntacticSingleKeywordRecommender _recommender;

            public TestAccessor(AbstractSyntacticSingleKeywordRecommender recommender)
                => _recommender = recommender;

            internal async Task<IEnumerable<RecommendedKeyword>> RecommendKeywordsAsync(int position, CSharpSyntaxContext context)
            {
                var syntaxKind = await _recommender.RecommendKeywordAsync(position, context, CancellationToken.None).ConfigureAwait(false);
                if (syntaxKind.HasValue)
                {
                    var matchPriority = _recommender.ShouldPreselect(context, CancellationToken.None) ? SymbolMatchPriority.Keyword : MatchPriority.Default;
                    return SpecializedCollections.SingletonEnumerable(
                        new RecommendedKeyword(SyntaxFacts.GetText(syntaxKind.Value), matchPriority: matchPriority));
                }

                return null;
            }
        }
    }
}
