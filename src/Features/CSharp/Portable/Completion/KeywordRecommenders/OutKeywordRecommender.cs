// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class OutKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public OutKeywordRecommender()
        : base(SyntaxKind.OutKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        var syntaxTree = context.SyntaxTree;

        return
            context.TargetToken.IsTypeParameterVarianceContext() ||
            IsOutParameterModifierContext(position, context) ||
            syntaxTree.IsAnonymousMethodParameterModifierContext(position, context.LeftToken) ||
            syntaxTree.IsPossibleLambdaParameterModifierContext(position, context.LeftToken, cancellationToken) ||
            context.TargetToken.IsConstructorOrMethodParameterArgumentContext() ||
            context.TargetToken.IsXmlCrefParameterModifierContext();
    }

    private static bool IsOutParameterModifierContext(int position, CSharpSyntaxContext context)
    {
        return context.SyntaxTree.IsParameterModifierContext(
                   position, context.LeftToken, includeOperators: false, out _, out var previousModifier) &&
               previousModifier is SyntaxKind.None or SyntaxKind.ScopedKeyword;
    }
}
