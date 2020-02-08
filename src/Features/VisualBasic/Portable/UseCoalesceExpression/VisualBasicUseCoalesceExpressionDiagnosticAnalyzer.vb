' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.UseCoalesceExpression
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCoalesceExpression
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseCoalesceExpressionDiagnosticAnalyzer
        Inherits AbstractUseCoalesceExpressionDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            TernaryConditionalExpressionSyntax,
            BinaryExpressionSyntax)

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function

        Protected Overrides Function GetSyntaxKindToAnalyze() As SyntaxKind
            Return SyntaxKind.TernaryConditionalExpression
        End Function

        Protected Overrides Function IsEquals(condition As BinaryExpressionSyntax) As Boolean
            Return condition.Kind() = SyntaxKind.IsExpression
        End Function

        Protected Overrides Function IsNotEquals(condition As BinaryExpressionSyntax) As Boolean
            Return condition.Kind() = SyntaxKind.IsNotExpression
        End Function
    End Class
End Namespace
