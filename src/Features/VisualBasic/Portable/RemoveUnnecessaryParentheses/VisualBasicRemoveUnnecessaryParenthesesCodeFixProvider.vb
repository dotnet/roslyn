' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses.ParenthesizedExpression
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryParentheses
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicRemoveUnnecessaryParenthesesCodeFixProvider
        Inherits AbstractRemoveUnnecessaryParenthesesCodeFixProvider(Of ParenthesizedExpressionSyntax)

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function

        Protected Overrides Function CanRemoveParentheses(current As ParenthesizedExpressionSyntax, semanticModel As SemanticModel) As Boolean
            Return VisualBasicRemoveUnnecessaryParenthesesDiagnosticAnalyzer.CanRemoveParenthesesHelper(
                current, semanticModel, precedence:=Nothing, clarifiesPrecedence:=Nothing)
        End Function
    End Class
End Namespace
