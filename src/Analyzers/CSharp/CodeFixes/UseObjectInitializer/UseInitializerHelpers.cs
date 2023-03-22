// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.UseObjectInitializer
{
    using static SyntaxFactory;

    internal static class UseInitializerHelpers
    {
        public static BaseObjectCreationExpressionSyntax GetNewObjectCreation(
            BaseObjectCreationExpressionSyntax baseObjectCreation,
            SeparatedSyntaxList<ExpressionSyntax> expressions)
        {
            if (baseObjectCreation is ObjectCreationExpressionSyntax objectCreation &&
                objectCreation.ArgumentList?.Arguments.Count == 0)
            {
                baseObjectCreation = objectCreation
                    .WithType(objectCreation.Type.WithTrailingTrivia(objectCreation.ArgumentList.GetTrailingTrivia()))
                    .WithArgumentList(null);
            }

            var firstExpression = expressions.First();
            var initializerKind = firstExpression is AssignmentExpressionSyntax
                ? SyntaxKind.ObjectInitializerExpression
                : SyntaxKind.CollectionInitializerExpression;

            return baseObjectCreation.WithInitializer(InitializerExpression(initializerKind, expressions));
        }

        public static void AddExistingItems(BaseObjectCreationExpressionSyntax objectCreation, ArrayBuilder<SyntaxNodeOrToken> nodesAndTokens)
        {
            if (objectCreation.Initializer != null)
                nodesAndTokens.AddRange(objectCreation.Initializer.Expressions.GetWithSeparators());

            // If we have an odd number of elements already, add a comma at the end so that we can add the rest of the
            // items afterwards without a syntax issue.
            if (nodesAndTokens.Count % 2 == 1)
            {
                var last = nodesAndTokens.Last();
                nodesAndTokens.RemoveLast();
                nodesAndTokens.Add(last.WithTrailingTrivia());
                nodesAndTokens.Add(Token(SyntaxKind.CommaToken).WithTrailingTrivia(last.GetTrailingTrivia()));
            }
        }
    }
}
