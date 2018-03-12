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
        Inherits AbstractAddRequiredParenthesesDiagnosticAnalyzer(Of SyntaxKind, BinaryExpressionSyntax)

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

        Protected Overrides Function GetSyntaxNodeKinds() As ImmutableArray(Of SyntaxKind)
            Return s_kinds
        End Function

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function

        Protected Overrides Function GetPrecedence(binaryExpression As BinaryExpressionSyntax) As Integer
            Return binaryExpression.GetOperatorPrecedence()
        End Function

        Protected Overrides Function GetPrecedenceKind(binaryExpression As BinaryExpressionSyntax, semanticModel As SemanticModel) As PrecedenceKind
            Return VisualBasicRemoveUnnecessaryParenthesesDiagnosticAnalyzer.GetPrecedenceKind(binaryExpression, semanticModel)
        End Function
    End Class
End Namespace
