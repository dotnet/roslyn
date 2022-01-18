// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal interface IKeywordRecommender<TContext>
        where TContext : SyntaxContext
    {
        ImmutableArray<RecommendedKeyword> RecommendKeywords(int position, TContext context, CancellationToken cancellationToken);
    }
}
