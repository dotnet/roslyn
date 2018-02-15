// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class LambdaExpressionSyntaxExtensions
    {
        public static LambdaExpressionSyntax WithBody(this LambdaExpressionSyntax node, CSharpSyntaxNode body)
        {
            if (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.SimpleLambdaExpression: return ((SimpleLambdaExpressionSyntax)node).WithBody(body);
                    case SyntaxKind.ParenthesizedLambdaExpression: return ((ParenthesizedLambdaExpressionSyntax)node).WithBody(body);
                }
            }

            return node;
        }
    }
}
