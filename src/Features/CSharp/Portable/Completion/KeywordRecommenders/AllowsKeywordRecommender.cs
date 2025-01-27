// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class AllowsKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public AllowsKeywordRecommender()
        : base(SyntaxKind.AllowsKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return IsAllowsRefStructConstraintContext(context);
    }

    private static bool IsAllowsRefStructConstraintContext(CSharpSyntaxContext context)
    {
        // cases:
        //    where T : |
        //    where T : struct, |
        //    where T : new(), |
        //    where T : Goo, |
        // note: 'allows ref struct' can't come after a 'class' constraint.

        if (context.SyntaxTree.IsTypeParameterConstraintStartContext(context.Position, context.LeftToken))
        {
            return true;
        }

        var token = context.TargetToken;

        if (token.Kind() == SyntaxKind.CommaToken &&
            token.Parent is TypeParameterConstraintClauseSyntax constraintClause)
        {
            if (!constraintClause.Constraints
                    .OfType<ClassOrStructConstraintSyntax>()
                    .Any(c => c.ClassOrStructKeyword.Kind() == SyntaxKind.ClassKeyword))
            {
                return true;
            }
        }

        return false;
    }
}
