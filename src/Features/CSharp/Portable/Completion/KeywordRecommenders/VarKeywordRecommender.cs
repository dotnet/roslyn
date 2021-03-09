// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class VarKeywordRecommender : IKeywordRecommender<CSharpSyntaxContext>
    {
        public VarKeywordRecommender()
        {
        }

        private static bool IsValidContext(CSharpSyntaxContext context)
        {
            if (context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                context.IsPossibleTupleContext ||
                context.IsAtStartOfPattern)
            {
                return true;
            }

            return context.IsLocalVariableDeclarationContext;
        }

        public ImmutableArray<RecommendedKeyword> RecommendKeywords(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return IsValidContext(context)
                ? ImmutableArray.Create(new RecommendedKeyword("var"))
                : ImmutableArray<RecommendedKeyword>.Empty;
        }
    }
}
