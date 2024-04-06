// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class NotNullKeywordRecommender : IKeywordRecommender<CSharpSyntaxContext>
{
    public ImmutableArray<RecommendedKeyword> RecommendKeywords(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return context.SyntaxTree.IsTypeParameterConstraintContext(position, context.LeftToken)
            ? [new RecommendedKeyword("notnull")]
            : [];
    }
}
