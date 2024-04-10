// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class OperatorKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public OperatorKeywordRecommender()
        : base(SyntaxKind.OperatorKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        // cases:
        //   public static implicit |
        //   public static explicit |
        var token = context.TargetToken;

        return
            token.Kind() is SyntaxKind.ImplicitKeyword or
            SyntaxKind.ExplicitKeyword;
    }
}
