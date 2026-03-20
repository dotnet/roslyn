// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class WithKeywordRecommender() : AbstractSyntacticSingleKeywordRecommender(SyntaxKind.WithKeyword)
{
    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsInNonUserCode)
            return false;

        if (context.IsIsOrAsOrSwitchOrWithExpressionContext)
            return true;

        var targetToken = context.TargetToken;
        if (targetToken.Kind() == SyntaxKind.OpenBracketToken &&
            targetToken.Parent is CollectionExpressionSyntax)
        {
            return true;
        }

        return false;
    }
}
