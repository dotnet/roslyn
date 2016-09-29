// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class VarKeywordRecommender : IKeywordRecommender<CSharpSyntaxContext>
    {
        public VarKeywordRecommender()
        {
        }

        private bool IsValidContext(CSharpSyntaxContext context)
        {
            if (context.IsStatementContext ||
                context.IsGlobalStatementContext)
            {
                return true;
            }

            return context.IsLocalVariableDeclarationContext;
        }

        public Task<IEnumerable<RecommendedKeyword>> RecommendKeywordsAsync(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (IsValidContext(context))
            {
                return Task.FromResult(SpecializedCollections.SingletonEnumerable(new RecommendedKeyword("var")));
            }

            return Task.FromResult<IEnumerable<RecommendedKeyword>>(null);
        }
    }
}
