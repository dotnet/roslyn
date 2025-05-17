' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Simplification

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.CodeFixes
    <ExportCodeFixProviderAttribute(LanguageNames.VisualBasic, Name:=NameOf(BasicApplyDiagnosticAnalyzerAttributeFix)), [Shared]>
    Public Class BasicApplyDiagnosticAnalyzerAttributeFix
        Inherits ApplyDiagnosticAnalyzerAttributeFix

        <ImportingConstructor>
        <Obsolete("This exported object must be obtained through the MEF export provider.", True)>
        Public Sub New()
        End Sub

        Protected Overrides Function ParseExpression(expression As String) As SyntaxNode
            Return SyntaxFactory.ParseExpression(expression).WithAdditionalAnnotations(Simplifier.Annotation)
        End Function
    End Class
End Namespace
