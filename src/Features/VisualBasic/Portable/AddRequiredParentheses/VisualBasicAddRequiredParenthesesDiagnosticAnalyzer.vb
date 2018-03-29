' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AddRequiredParentheses
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryParentheses
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddRequiredParentheses
    <DiagnosticAnalyzer(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicAddRequiredParenthesesForBinaryLikeExpressionDiagnosticAnalyzer
        Inherits AbstractAddRequiredParenthesesForBinaryLikeExpressionDiagnosticAnalyzer(Of
            ExpressionSyntax, BinaryExpressionSyntax, SyntaxKind)

        Private Shared ReadOnly s_kinds As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(
                SyntaxKind.AddExpression,
                SyntaxKind.SubtractExpression,
                SyntaxKind.MultiplyExpression,
                SyntaxKind.DivideExpression,
                SyntaxKind.IntegerDivideExpression,
                SyntaxKind.ExponentiateExpression,
                SyntaxKind.LeftShiftExpression,
                SyntaxKind.RightShiftExpression,
                SyntaxKind.ConcatenateExpression,
                SyntaxKind.ModuloExpression,
                SyntaxKind.EqualsExpression,
                SyntaxKind.NotEqualsExpression,
                SyntaxKind.LessThanExpression,
                SyntaxKind.LessThanOrEqualExpression,
                SyntaxKind.GreaterThanOrEqualExpression,
                SyntaxKind.GreaterThanExpression,
                SyntaxKind.IsExpression,
                SyntaxKind.IsNotExpression,
                SyntaxKind.LikeExpression,
                SyntaxKind.OrExpression,
                SyntaxKind.ExclusiveOrExpression,
                SyntaxKind.AndExpression,
                SyntaxKind.OrElseExpression,
                SyntaxKind.AndAlsoExpression)

        Protected Overrides Sub GetPartsOfBinaryLike(
                binary As BinaryExpressionSyntax, ByRef left As ExpressionSyntax, ByRef operatorToken As SyntaxToken, ByRef right As ExpressionSyntax)
            left = binary.Left
            operatorToken = binary.OperatorToken
            right = binary.Right
        End Sub

        Protected Overrides Function GetSyntaxNodeKinds() As ImmutableArray(Of SyntaxKind)
            Return s_kinds
        End Function

        Protected Overrides Function GetPrecedence(binary As BinaryExpressionSyntax) As Integer
            Return binary.GetOperatorPrecedence()
        End Function

        Protected Overrides Function GetPrecedenceKind(binary As BinaryExpressionSyntax) As PrecedenceKind
            Return VisualBasicRemoveUnnecessaryParenthesesDiagnosticAnalyzer.GetPrecedenceKind(binary)
        End Function

        Protected Overrides Function TryGetParentExpression(binary As BinaryExpressionSyntax) As ExpressionSyntax
            Return TryCast(binary.Parent, ExpressionSyntax)
        End Function

        Protected Overrides Function IsBinaryLike(node As ExpressionSyntax) As Boolean
            Return TypeOf node Is BinaryExpressionSyntax
        End Function
    End Class
End Namespace
