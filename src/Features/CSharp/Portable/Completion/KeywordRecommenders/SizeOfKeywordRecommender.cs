// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class SizeOfKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public SizeOfKeywordRecommender()
        : base(SyntaxKind.SizeOfKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            context.IsNonAttributeExpressionContext ||
            context.IsStatementContext ||
            context.IsGlobalStatementContext;
    }
}
