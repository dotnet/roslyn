// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class OnKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public OnKeywordRecommender()
        : base(SyntaxKind.OnKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        // cases:
        //   join a in expr |
        //   join a in expr o|
        //   join a.b c in expr |
        //   join a.b c in expr o|

        var token = context.TargetToken;

        var join = token.GetAncestor<JoinClauseSyntax>();
        if (join == null)
        {
            return false;
        }

        // join a in expr |
        // join a.b c in expr |

        var lastToken = join.InExpression.GetLastToken(includeSkipped: true);

        if (join.InExpression.Width() > 0 &&
            token == lastToken)
        {
            return true;
        }

        return false;
    }
}
