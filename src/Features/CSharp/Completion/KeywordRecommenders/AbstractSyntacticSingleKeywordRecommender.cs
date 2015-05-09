// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
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

        protected abstract bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken);

        public IEnumerable<RecommendedKeyword> RecommendKeywords(
            int position,
            CSharpSyntaxContext context,
            CancellationToken cancellationToken)
        {
            var syntaxKind = this.RecommendKeyword(position, context, cancellationToken);
            if (syntaxKind.HasValue)
            {
                return SpecializedCollections.SingletonEnumerable(
                    new RecommendedKeyword(SyntaxFacts.GetText(syntaxKind.Value), shouldFormatOnCommit: this.ShouldFormatOnCommit));
            }

            return null;
        }

        internal IEnumerable<RecommendedKeyword> RecommendKeywords_Test(int position, CSharpSyntaxContext context)
        {
            var syntaxKind = this.RecommendKeyword(position, context, CancellationToken.None);
            if (syntaxKind.HasValue)
            {
                return SpecializedCollections.SingletonEnumerable(
                    new RecommendedKeyword(SyntaxFacts.GetText(syntaxKind.Value)));
            }

            return null;
        }

        private SyntaxKind? RecommendKeyword(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // NOTE: The collector ensures that we're not in "NonUserCode" like comments, strings, inactive code
            // for perf reasons.
            var syntaxTree = context.SemanticModel.SyntaxTree;
            if (!_isValidInPreprocessorContext &&
                context.IsPreProcessorDirectiveContext)
            {
                return null;
            }

            if (!IsValidContext(position, context, cancellationToken))
            {
                return null;
            }

            return this.KeywordKind;
        }
    }
}
