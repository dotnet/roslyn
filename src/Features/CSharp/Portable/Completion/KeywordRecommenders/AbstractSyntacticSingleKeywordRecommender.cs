// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal abstract partial class AbstractSyntacticSingleKeywordRecommender : IKeywordRecommender<CSharpSyntaxContext>
    {
        public readonly SyntaxKind KeywordKind;
        private readonly bool _isValidInPreprocessorContext;
        private readonly bool _shouldFormatOnCommit;

        protected AbstractSyntacticSingleKeywordRecommender(
            SyntaxKind keywordKind,
            bool isValidInPreprocessorContext = false,
            bool shouldFormatOnCommit = false)
        {
            KeywordKind = keywordKind;
            _isValidInPreprocessorContext = isValidInPreprocessorContext;
            _shouldFormatOnCommit = shouldFormatOnCommit;
        }

        protected abstract bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken);

        public ImmutableArray<RecommendedKeyword> RecommendKeywords(
            int position,
            CSharpSyntaxContext context,
            CancellationToken cancellationToken)
        {
            var syntaxKind = RecommendKeyword(position, context, cancellationToken);
            if (syntaxKind.HasValue)
            {
                return ImmutableArray.Create(
                    new RecommendedKeyword(SyntaxFacts.GetText(syntaxKind.Value),
                        shouldFormatOnCommit: _shouldFormatOnCommit,
                        matchPriority: ShouldPreselect(context, cancellationToken) ? SymbolMatchPriority.Keyword : MatchPriority.Default));
            }

            return ImmutableArray<RecommendedKeyword>.Empty;
        }

        protected virtual bool ShouldPreselect(CSharpSyntaxContext context, CancellationToken cancellationToken) => false;

        private SyntaxKind? RecommendKeyword(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // NOTE: The collector ensures that we're not in "NonUserCode" like comments, strings, inactive code
            // for perf reasons.
            if (!_isValidInPreprocessorContext &&
                context.IsPreProcessorDirectiveContext)
            {
                return null;
            }

            return IsValidContext(position, context, cancellationToken) ? KeywordKind : null;
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly AbstractSyntacticSingleKeywordRecommender _recommender;

            public TestAccessor(AbstractSyntacticSingleKeywordRecommender recommender)
                => _recommender = recommender;

            internal ImmutableArray<RecommendedKeyword> RecommendKeywords(int position, CSharpSyntaxContext context)
            {
                var syntaxKind = _recommender.RecommendKeyword(position, context, CancellationToken.None);
                if (syntaxKind.HasValue)
                {
                    var matchPriority = _recommender.ShouldPreselect(context, CancellationToken.None) ? SymbolMatchPriority.Keyword : MatchPriority.Default;
                    return ImmutableArray.Create(new RecommendedKeyword(SyntaxFacts.GetText(syntaxKind.Value), matchPriority: matchPriority));
                }

                return ImmutableArray<RecommendedKeyword>.Empty;
            }
        }
    }
}
