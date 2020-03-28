// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UseObjectInitializer
{
    internal static class UseInitializerHelpers
    {
        public static ObjectCreationExpressionSyntax GetNewObjectCreation(
            ObjectCreationExpressionSyntax objectCreation,
            SeparatedSyntaxList<ExpressionSyntax> expressions)
        {
            var openBrace = SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                                         .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);
            var initializer = SyntaxFactory.InitializerExpression(
                SyntaxKind.ObjectInitializerExpression, expressions).WithOpenBraceToken(openBrace);

            if (objectCreation.ArgumentList != null &&
                objectCreation.ArgumentList.Arguments.Count == 0)
            {
                objectCreation = objectCreation.WithType(objectCreation.Type.WithTrailingTrivia(objectCreation.ArgumentList.GetTrailingTrivia()))
                                               .WithArgumentList(null);
            }

            return objectCreation.WithInitializer(initializer);
        }
    }
}
