// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class AliasKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public AliasKeywordRecommender()
        : base(SyntaxKind.AliasKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        // cases:
        //   extern |
        //   extern a|
        var token = context.TargetToken;

        if (token.Kind() == SyntaxKind.ExternKeyword)
        {
            // members can be 'extern' but we don't want
            // 'alias' to show up in a 'type'.
            return token.GetAncestor<TypeDeclarationSyntax>() == null;
        }

        return false;
    }
}
