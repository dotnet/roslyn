' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.AddRequiredParentheses
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Precedence
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddRequiredParentheses
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicAddRequiredParenthesesForBinaryLikeExpressionDiagnosticAnalyzer
        Inherits AbstractAddRequiredParenthesesDiagnosticAnalyzer(Of
            ExpressionSyntax, BinaryExpressionSyntax, SyntaxKind)

        Public Sub New()
            MyBase.New(VisualBasicPrecedenceService.Instance)
        End Sub

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

        Protected Overrides Function GetPartsOfBinaryLike(binaryLike As BinaryExpressionSyntax) As (ExpressionSyntax, SyntaxToken, ExpressionSyntax)
            Return (binaryLike.Left, binaryLike.OperatorToken, binaryLike.Right)
        End Function

        Protected Overrides Function GetSyntaxNodeKinds() As ImmutableArray(Of SyntaxKind)
            Return s_kinds
        End Function

        Protected Overrides Function GetPrecedence(binary As BinaryExpressionSyntax) As Integer
            Return binary.GetOperatorPrecedence()
        End Function

        Protected Overrides Function TryGetAppropriateParent(binary As BinaryExpressionSyntax) As ExpressionSyntax
            Return TryCast(binary.Parent, ExpressionSyntax)
        End Function

        Protected Overrides Function IsBinaryLike(node As ExpressionSyntax) As Boolean
            Return TypeOf node Is BinaryExpressionSyntax
        End Function

        Protected Overrides Function IsAsExpression(node As BinaryExpressionSyntax) As Boolean
            ' VB does not have an 'as' node that is a binary-expression like C# does. Instead, it is TryCast(a, b)
            ' which is not a binary expression and never needs extra parentheses.
            Return False
        End Function
    End Class
End Namespace
