// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
            this.KeywordKind = keywordKind;
            _isValidInPreprocessorContext = isValidInPreprocessorContext;
            this.ShouldFormatOnCommit = shouldFormatOnCommit;
        }

        protected virtual Task<bool> IsValidContextAsync(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(IsValidContext(position, context, cancellationToken));
        }

        protected virtual bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return false;
        }

        public async Task<IEnumerable<RecommendedKeyword>> RecommendKeywordsAsync(
            int position,
            CSharpSyntaxContext context,
            CancellationToken cancellationToken)
        {
            var syntaxKind = await this.RecommendKeywordAsync(position, context, cancellationToken).ConfigureAwait(false);
            if (syntaxKind.HasValue)
            {
                return SpecializedCollections.SingletonEnumerable(
                    new RecommendedKeyword(SyntaxFacts.GetText(syntaxKind.Value), shouldFormatOnCommit: this.ShouldFormatOnCommit));
            }

            return null;
        }

        internal async Task<IEnumerable<RecommendedKeyword>> RecommendKeywordsAsync_Test(int position, CSharpSyntaxContext context)
        {
            var syntaxKind = await this.RecommendKeywordAsync(position, context, CancellationToken.None).ConfigureAwait(false);
            if (syntaxKind.HasValue)
            {
                return SpecializedCollections.SingletonEnumerable(
                    new RecommendedKeyword(SyntaxFacts.GetText(syntaxKind.Value)));
            }

            return null;
        }

        private async Task<SyntaxKind?> RecommendKeywordAsync(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // NOTE: The collector ensures that we're not in "NonUserCode" like comments, strings, inactive code
            // for perf reasons.
            var syntaxTree = context.SemanticModel.SyntaxTree;
            if (!_isValidInPreprocessorContext &&
                context.IsPreProcessorDirectiveContext)
            {
                return null;
            }

            if (!await IsValidContextAsync(position, context, cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            return this.KeywordKind;
        }
    }
}
