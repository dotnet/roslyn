// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal abstract class AbstractSpecialTypePreselectingKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public AbstractSpecialTypePreselectingKeywordRecommender(
            SyntaxKind keywordKind,
            bool isValidInPreprocessorContext = false,
            bool shouldFormatOnCommit = false)
            : base(keywordKind, isValidInPreprocessorContext, shouldFormatOnCommit)
        {
        }

        protected abstract SpecialType SpecialType { get; }
        protected abstract bool IsValidContextWorker(int position, CSharpSyntaxContext context, CancellationToken cancellationToken);

        // When the keyword is the inferred type in this context, we should treat it like its corresponding type symbol
        // in terms of MatchPripority, so the selection can be determined by how well it matches the filter text instead,
        // e.g. selecting "string" over "String" when user typed "str".
        protected override int PreselectMatchPriority => SymbolMatchPriority.PreferType;

        protected override bool ShouldPreselect(CSharpSyntaxContext context, CancellationToken cancellationToken)
            => context.InferredTypes.Any(static (t, self) => t.SpecialType == self.SpecialType, this);

        protected sealed override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // Filter out all special-types from locations where we think we only want something task-like.
            if (context.IsTaskLikeTypeContext)
                return false;

            return IsValidContextWorker(position, context, cancellationToken);
        }
    }
}
