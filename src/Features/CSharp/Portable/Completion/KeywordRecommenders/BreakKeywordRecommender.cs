// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class BreakKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public BreakKeywordRecommender()
        : base(SyntaxKind.BreakKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            IsInBreakableConstructContext(context) ||
            context.TargetToken.IsAfterYieldKeyword();
    }

    private static bool IsInBreakableConstructContext(CSharpSyntaxContext context)
    {
        if (!context.IsStatementContext)
        {
            return false;
        }

        // allowed if we're inside a loop/switch construct.

        var token = context.LeftToken;
        foreach (var v in token.GetAncestors<SyntaxNode>())
        {
            if (v is AnonymousFunctionExpressionSyntax)
            {
                // if we hit a lambda while walking up, then we can't
                // 'continue' any outer loops.
                return false;
            }

            if (v.IsBreakableConstruct())
            {
                return true;
            }
        }

        return false;
    }
}
