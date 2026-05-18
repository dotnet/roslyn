// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal static class UseInitializerHelpers
{
    public static BaseObjectCreationExpressionSyntax GetNewObjectCreation(
        BaseObjectCreationExpressionSyntax baseObjectCreation,
        SeparatedSyntaxList<ExpressionSyntax> expressions)
    {
        if (baseObjectCreation is ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0 } objectCreation)
        {
            baseObjectCreation = objectCreation
                .WithType(objectCreation.Type.WithTrailingTrivia(objectCreation.ArgumentList.GetTrailingTrivia()))
                .WithArgumentList(null);
        }

        // Pick the wrapper kind the way the parser would: any assignment-shape element flips the
        // wrapper to `ObjectInitializerExpression` (including the mixed object/collection
        // initializer form, dotnet/csharplang#10185, where assignment- and element-shape children
        // appear together). Scanning all expressions (rather than only the first) ensures we agree
        // with the parser even when the first element happens to be the bare-expression form.
        var hasAssignment = false;
        foreach (var expression in expressions)
        {
            if (expression is AssignmentExpressionSyntax)
            {
                hasAssignment = true;
                break;
            }
        }

        var initializerKind = hasAssignment
            ? SyntaxKind.ObjectInitializerExpression
            : SyntaxKind.CollectionInitializerExpression;

        return baseObjectCreation.WithInitializer(InitializerExpression(initializerKind, expressions));
    }

    public static void AddExistingItems<TMatch, TElementSyntax>(
        BaseObjectCreationExpressionSyntax objectCreation,
        ArrayBuilder<SyntaxNodeOrToken> nodesAndTokens,
        bool addTrailingComma,
        Func<TMatch?, ExpressionSyntax, TElementSyntax> createElement)
        where TMatch : struct
        where TElementSyntax : SyntaxNode
    {
        if (objectCreation.Initializer != null)
        {
            foreach (var nodeOrToken in objectCreation.Initializer.Expressions.GetWithSeparators())
            {
                if (nodeOrToken.IsToken)
                    nodesAndTokens.Add(nodeOrToken.AsToken());
                else
                    nodesAndTokens.Add(createElement(null, (ExpressionSyntax)nodeOrToken.AsNode()!));
            }
        }

        // If we have an odd number of elements already, add a comma at the end so that we can add the rest of the
        // items afterwards without a syntax issue.
        if (addTrailingComma && nodesAndTokens.Count % 2 == 1)
        {
            var last = nodesAndTokens.Last();
            nodesAndTokens.RemoveLast();
            nodesAndTokens.Add(last.WithTrailingTrivia());
            nodesAndTokens.Add(CommaToken.WithTrailingTrivia(last.GetTrailingTrivia()));
        }
    }
}
