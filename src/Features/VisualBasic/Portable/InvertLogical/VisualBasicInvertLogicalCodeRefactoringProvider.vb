' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.InvertLogical

Namespace Microsoft.CodeAnalysis.VisualBasic.InvertLogical
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicInvertLogicalCodeRefactoringProvider
        Inherits AbstractInvertLogicalCodeRefactoringProvider(Of SyntaxKind, ExpressionSyntax, BinaryExpressionSyntax)

        Protected Overrides Function GetKind(rawKind As Integer) As SyntaxKind
            Return CType(rawKind, SyntaxKind)
        End Function

        Protected Overrides Function InvertedKind(binaryExprKind As SyntaxKind) As SyntaxKind
            Return If(binaryExprKind = SyntaxKind.AndAlsoExpression,
                      SyntaxKind.OrElseExpression,
                      SyntaxKind.AndAlsoExpression)
        End Function

        Protected Overrides Function GetOperatorTokenKind(binaryExprKind As SyntaxKind) As SyntaxKind
            Return If(binaryExprKind = SyntaxKind.AndAlsoExpression,
                      SyntaxKind.AndAlsoKeyword,
                      SyntaxKind.OrElseKeyword)
        End Function

        Protected Overrides Function CreateOperatorToken(operatorTokenKind As SyntaxKind) As SyntaxToken
            Return SyntaxFactory.Token(operatorTokenKind)
        End Function

        Protected Overrides Function BinaryExpression(
                syntaxKind As SyntaxKind, newLeft As ExpressionSyntax, newOp As SyntaxToken, newRight As ExpressionSyntax) As BinaryExpressionSyntax

            Return SyntaxFactory.BinaryExpression(syntaxKind, newLeft, newOp, newRight)
        End Function
    End Class
End Namespace
