// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        /// <summary>Creates a new LiteralExpressionSyntax instance.</summary>
        public static LiteralExpressionSyntax LiteralExpression(SyntaxKind kind)
            => SyntaxFactory.LiteralExpression(kind, SyntaxFactory.Token(GetLiteralExpressionTokenKind(kind)));

        private static SyntaxKind GetLiteralExpressionTokenKind(SyntaxKind kind)
            => kind switch
            {
                SyntaxKind.ArgListExpression => SyntaxKind.ArgListKeyword,
                SyntaxKind.NumericLiteralExpression => SyntaxKind.NumericLiteralToken,
                SyntaxKind.StringLiteralExpression => SyntaxKind.StringLiteralToken,
                SyntaxKind.UTF8StringLiteralExpression => SyntaxKind.UTF8StringLiteralToken,
                SyntaxKind.CharacterLiteralExpression => SyntaxKind.CharacterLiteralToken,
                SyntaxKind.TrueLiteralExpression => SyntaxKind.TrueKeyword,
                SyntaxKind.FalseLiteralExpression => SyntaxKind.FalseKeyword,
                SyntaxKind.NullLiteralExpression => SyntaxKind.NullKeyword,
                SyntaxKind.DefaultLiteralExpression => SyntaxKind.DefaultKeyword,
                _ => throw new ArgumentOutOfRangeException(),
            };
    }
}
