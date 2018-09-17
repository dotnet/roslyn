// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.InvertLogical;

namespace Microsoft.CodeAnalysis.CSharp.InvertLogical
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class CSharpInvertLogicalCodeRefactoringProvider : 
        AbstractInvertLogicalCodeRefactoringProvider<SyntaxKind, ExpressionSyntax, BinaryExpressionSyntax>
    {
        protected override SyntaxKind GetKind(int rawKind)
            => (SyntaxKind)rawKind;

        protected override SyntaxKind InvertedKind(SyntaxKind binaryExprKind)
            => binaryExprKind == SyntaxKind.LogicalAndExpression
                ? SyntaxKind.LogicalOrExpression
                : SyntaxKind.LogicalAndExpression;

        protected override SyntaxKind GetOperatorTokenKind(SyntaxKind binaryExprKind)
            => binaryExprKind == SyntaxKind.LogicalAndExpression
                ? SyntaxKind.AmpersandAmpersandToken
                : SyntaxKind.BarBarToken;

        protected override SyntaxToken CreateOperatorToken(SyntaxKind operatorTokenKind)
            => SyntaxFactory.Token(operatorTokenKind);

        protected override BinaryExpressionSyntax BinaryExpression(
            SyntaxKind syntaxKind, ExpressionSyntax newLeft, SyntaxToken newOp, ExpressionSyntax newRight)
            => SyntaxFactory.BinaryExpression(syntaxKind, newLeft, newOp, newRight);
    }
}
