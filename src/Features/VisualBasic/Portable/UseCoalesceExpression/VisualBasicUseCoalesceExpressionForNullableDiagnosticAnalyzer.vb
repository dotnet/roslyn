' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.UseCoalesceExpression
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCoalesceExpression
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseCoalesceExpressionForNullableDiagnosticAnalyzer
        Inherits AbstractUseCoalesceExpressionForNullableDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            TernaryConditionalExpressionSyntax,
            BinaryExpressionSyntax,
            MemberAccessExpressionSyntax,
            UnaryExpressionSyntax)

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function

        Protected Overrides Function GetSyntaxKindToAnalyze() As SyntaxKind
            Return SyntaxKind.TernaryConditionalExpression
        End Function
    End Class
End Namespace
