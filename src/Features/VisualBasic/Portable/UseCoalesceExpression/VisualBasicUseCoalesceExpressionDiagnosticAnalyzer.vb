' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
