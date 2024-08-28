// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class StackAllocKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public StackAllocKeywordRecommender()
        : base(SyntaxKind.StackAllocKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        // Beginning with C# 8.0, stackalloc expression can be used inside other expressions
        // whenever a Span<T> or ReadOnlySpan<T> variable is allowed.
        return (context.IsAnyExpressionContext && !context.IsConstantExpressionContext) ||
                   context.IsStatementContext ||
                   context.IsGlobalStatementContext;
    }
}
