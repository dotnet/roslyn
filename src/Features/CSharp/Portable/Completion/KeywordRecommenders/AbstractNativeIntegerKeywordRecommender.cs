// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal abstract class AbstractNativeIntegerKeywordRecommender : IKeywordRecommender<CSharpSyntaxContext>
    {
        protected abstract RecommendedKeyword Keyword { get; }

        private static bool IsValidContext(CSharpSyntaxContext context)
        {
            if (context.IsInTaskLikeTypeContext)
                return false;

            if (context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                context.IsPossibleTupleContext ||
                context.IsAtStartOfPattern ||
                (context.IsTypeContext && !context.IsEnumBaseListContext))
            {
                return true;
            }

            return context.IsLocalVariableDeclarationContext;
        }

        public ImmutableArray<RecommendedKeyword> RecommendKeywords(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
            => IsValidContext(context) ? ImmutableArray.Create(Keyword) : ImmutableArray<RecommendedKeyword>.Empty;
    }
}
