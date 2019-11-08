// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            if (objectCreation is { ArgumentList: { Arguments: { Count: 0 } } })
            {
                objectCreation = objectCreation.WithType(objectCreation.Type.WithTrailingTrivia(objectCreation.ArgumentList.GetTrailingTrivia()))
                                               .WithArgumentList(null);
            }

            return objectCreation.WithInitializer(initializer);
        }
    }
}
