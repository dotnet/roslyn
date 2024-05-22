// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal abstract partial class AbstractSyntacticSingleKeywordRecommender : IKeywordRecommender<CSharpSyntaxContext>
{
    public readonly SyntaxKind KeywordKind;
    private readonly bool _isValidInPreprocessorContext;

    private readonly ImmutableArray<RecommendedKeyword> _keywordPriorityRecommendedKeywords;
    private readonly ImmutableArray<RecommendedKeyword> _defaultPriorityRecommendedKeywords;

    /// <summary>
    /// Matching priority for the provided item when <see cref="ShouldPreselect"/> returns <see langword="false"/>.
    /// </summary>
    protected virtual int DefaultMatchPriority => MatchPriority.Default;

    protected virtual int PreselectMatchPriority => SymbolMatchPriority.Keyword;

    protected AbstractSyntacticSingleKeywordRecommender(
        SyntaxKind keywordKind,
        bool isValidInPreprocessorContext = false,
        bool shouldFormatOnCommit = false)
    {
        KeywordKind = keywordKind;
        _isValidInPreprocessorContext = isValidInPreprocessorContext;

        _keywordPriorityRecommendedKeywords = [new RecommendedKeyword(SyntaxFacts.GetText(keywordKind),
            shouldFormatOnCommit: shouldFormatOnCommit,
            matchPriority: PreselectMatchPriority)];
        _defaultPriorityRecommendedKeywords = [new RecommendedKeyword(SyntaxFacts.GetText(keywordKind),
            shouldFormatOnCommit: shouldFormatOnCommit,
            matchPriority: DefaultMatchPriority)];
    }

    protected abstract bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken);

    public ImmutableArray<RecommendedKeyword> RecommendKeywords(
        int position,
        CSharpSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var syntaxKind = RecommendKeyword(position, context, cancellationToken);
        if (!syntaxKind.HasValue)
            return [];

        return ShouldPreselect(context, cancellationToken)
            ? _keywordPriorityRecommendedKeywords
            : _defaultPriorityRecommendedKeywords;
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

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(AbstractSyntacticSingleKeywordRecommender recommender)
    {
        private readonly AbstractSyntacticSingleKeywordRecommender _recommender = recommender;

        public ImmutableArray<RecommendedKeyword> RecommendKeywords(int position, CSharpSyntaxContext context)
            => _recommender.RecommendKeywords(position, context, CancellationToken.None);
    }
}
