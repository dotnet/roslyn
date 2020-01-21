// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class UnmanagedKeywordRecommender : IKeywordRecommender<CSharpSyntaxContext>
    {
        public Task<IEnumerable<RecommendedKeyword>> RecommendKeywordsAsync(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.SyntaxTree.IsTypeParameterConstraintContext(position, context.LeftToken))
            {
                return Task.FromResult(SpecializedCollections.SingletonEnumerable(new RecommendedKeyword("unmanaged")));
            }

            return Task.FromResult<IEnumerable<RecommendedKeyword>>(null);
        }
    }
}
