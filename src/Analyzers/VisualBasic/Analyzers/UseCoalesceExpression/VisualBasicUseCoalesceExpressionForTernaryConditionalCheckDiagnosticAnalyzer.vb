' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.UseCoalesceExpression
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCoalesceExpression
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseCoalesceExpressionForTernaryConditionalCheckDiagnosticAnalyzer
        Inherits AbstractUseCoalesceExpressionForTernaryConditionalCheckDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            TernaryConditionalExpressionSyntax,
            BinaryExpressionSyntax)

        Protected Overrides Function GetSyntaxFacts() As ISyntaxFacts
            Return VisualBasicSyntaxFacts.Instance
        End Function

        Protected Overrides Function IsTargetTyped(semanticModel As SemanticModel, conditional As TernaryConditionalExpressionSyntax, cancellationToken As CancellationToken) As Boolean
            ' VB does not have target typed conditionals.
            Return False
        End Function
    End Class
End Namespace
