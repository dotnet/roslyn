﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class NotNullKeywordRecommender : IKeywordRecommender<CSharpSyntaxContext>
    {
        public Task<IEnumerable<RecommendedKeyword>> RecommendKeywordsAsync(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.SyntaxTree.IsTypeParameterConstraintContext(position, context.LeftToken))
            {
                return Task.FromResult(SpecializedCollections.SingletonEnumerable(new RecommendedKeyword("notnull")));
            }

            return SpecializedTasks.Null<IEnumerable<RecommendedKeyword>>();
        }
    }
}
