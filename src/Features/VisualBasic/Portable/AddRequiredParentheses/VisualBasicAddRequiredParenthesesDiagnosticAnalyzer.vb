' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AddRequiredParentheses
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryParentheses
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddRequiredParentheses
    <DiagnosticAnalyzer(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicAddRequiredParenthesesDiagnosticAnalyzer
        Inherits AbstractAddRequiredParenthesesDiagnosticAnalyzer(Of SyntaxKind)

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
                SyntaxKind.AndAlsoExpression,
                SyntaxKind.SimpleAssignmentStatement,
                SyntaxKind.AddAssignmentStatement,
                SyntaxKind.SubtractAssignmentStatement,
                SyntaxKind.MultiplyAssignmentStatement,
                SyntaxKind.DivideAssignmentStatement,
                SyntaxKind.IntegerDivideAssignmentStatement,
                SyntaxKind.ExponentiateAssignmentStatement,
                SyntaxKind.LeftShiftAssignmentStatement,
                SyntaxKind.RightShiftAssignmentStatement,
                SyntaxKind.ConcatenateAssignmentStatement)

        Protected Overrides Sub GetPartsOfBinaryLike(
                binaryLike As SyntaxNode, ByRef left As SyntaxNode, ByRef operatorToken As SyntaxToken, ByRef right As SyntaxNode)
            Dim binary = TryCast(binaryLike, BinaryExpressionSyntax)
            If binary IsNot Nothing Then
                left = binary.Left
                operatorToken = binary.OperatorToken
                right = binary.Right
            Else
                Dim assignment = DirectCast(binaryLike, AssignmentStatementSyntax)
                left = assignment.Left
                operatorToken = assignment.OperatorToken
                right = assignment.Right
            End If
        End Sub

        Protected Overrides Function GetSyntaxNodeKinds() As ImmutableArray(Of SyntaxKind)
            Return s_kinds
        End Function

        Protected Overrides Function GetPrecedence(binaryLike As SyntaxNode) As Integer
            Dim binary = TryCast(binaryLike, BinaryExpressionSyntax)
            If binary IsNot Nothing Then
                Return binary.GetOperatorPrecedence()
            Else
                ' VB has no actual precedence for assignment (because it's a statement).  Our caller 
                ' just needs this value to be different than all the actual precedence values, so
                ' we just return -1 to keep things simple here.
                Return -1
            End If
        End Function

        Protected Overrides Function GetPrecedenceKind(binaryLike As SyntaxNode) As PrecedenceKind
            Return VisualBasicRemoveUnnecessaryParenthesesDiagnosticAnalyzer.GetPrecedenceKind(binaryLike)
        End Function

        Protected Overrides Function GetParentExpressionOrAssignment(binaryLikeExpression As SyntaxNode) As SyntaxNode
            Return binaryLikeExpression.Parent
        End Function

        Protected Overrides Function IsBinaryLike(node As SyntaxNode) As Boolean
            Return TypeOf node Is BinaryExpressionSyntax OrElse
                   TypeOf node Is AssignmentStatementSyntax
        End Function
    End Class
End Namespace
