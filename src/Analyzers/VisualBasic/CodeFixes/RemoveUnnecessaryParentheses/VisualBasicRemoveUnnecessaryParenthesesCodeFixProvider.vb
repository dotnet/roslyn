' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryParentheses
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveUnnecessaryParentheses), [Shared]>
    Friend Class VisualBasicRemoveUnnecessaryParenthesesCodeFixProvider
        Inherits AbstractRemoveUnnecessaryParenthesesCodeFixProvider(Of ParenthesizedExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function CanRemoveParentheses(current As ParenthesizedExpressionSyntax, semanticModel As SemanticModel, cancellationtoken As CancellationToken) As Boolean
            Return VisualBasicRemoveUnnecessaryParenthesesDiagnosticAnalyzer.CanRemoveParenthesesHelper(
                current, semanticModel, precedence:=Nothing, clarifiesPrecedence:=Nothing, innerExpressionHasPrimaryPrecedence:=Nothing)
        End Function
    End Class
End Namespace
