﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class ByKeywordRecommender() : AbstractSyntacticSingleKeywordRecommender(SyntaxKind.ByKeyword)
{
    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        // cases:
        //   group e |
        //   group e b|

        var token = context.LeftToken;
        var group = token.GetAncestor<GroupClauseSyntax>();

        if (group == null)
        {
            return false;
        }

        var lastToken = group.GroupExpression.GetLastToken(includeSkipped: true);

        // group e |
        if (!token.IntersectsWith(position) &&
            token == lastToken)
        {
            return true;
        }

        // group e b|
        if (token.IntersectsWith(position) &&
            token.Kind() == SyntaxKind.IdentifierToken &&
            token.GetPreviousToken(includeSkipped: true) == lastToken)
        {
            return true;
        }

        return false;
    }
}
