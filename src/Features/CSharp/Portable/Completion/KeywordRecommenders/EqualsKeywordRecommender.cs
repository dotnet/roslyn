// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class EqualsKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public EqualsKeywordRecommender()
        : base(SyntaxKind.EqualsKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        // cases:
        //   join a in expr o1 |
        //   join a in expr o1 e|

        var token = context.TargetToken;

        var join = token.GetAncestor<JoinClauseSyntax>();
        if (join == null)
        {
            return false;
        }

        var lastToken = join.LeftExpression.GetLastToken(includeSkipped: true);

        // join a in expr |
        if (join.LeftExpression.Width() > 0 &&
            token == lastToken)
        {
            return true;
        }

        return false;
    }
}
