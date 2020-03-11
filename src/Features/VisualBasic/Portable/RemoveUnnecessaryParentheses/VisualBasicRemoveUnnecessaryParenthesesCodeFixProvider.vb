﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryParentheses
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicRemoveUnnecessaryParenthesesCodeFixProvider
        Inherits AbstractRemoveUnnecessaryParenthesesCodeFixProvider(Of ParenthesizedExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function CanRemoveParentheses(current As ParenthesizedExpressionSyntax, semanticModel As SemanticModel) As Boolean
            Return VisualBasicRemoveUnnecessaryParenthesesDiagnosticAnalyzer.CanRemoveParenthesesHelper(
                current, semanticModel, precedence:=Nothing, clarifiesPrecedence:=Nothing)
        End Function
    End Class
End Namespace
