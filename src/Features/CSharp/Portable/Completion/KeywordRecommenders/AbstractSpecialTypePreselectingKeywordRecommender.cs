// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
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

        protected override bool ShouldPreselect(CSharpSyntaxContext context, CancellationToken cancellationToken)
            => context.InferredTypes.Any(t => t.SpecialType == SpecialType);

        protected sealed override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // Filter out all special-types from locations where we think we only want something task-like.
            if (context.IsInTaskLikeTypeContext)
                return false;

            return IsValidContextWorker(position, context, cancellationToken);
        }
    }
}
