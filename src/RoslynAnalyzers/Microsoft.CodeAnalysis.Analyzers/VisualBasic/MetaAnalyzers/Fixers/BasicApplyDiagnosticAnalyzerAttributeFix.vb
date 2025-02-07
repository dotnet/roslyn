' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Simplification

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.CodeFixes
    <ExportCodeFixProviderAttribute(LanguageNames.VisualBasic, Name:=NameOf(BasicApplyDiagnosticAnalyzerAttributeFix)), [Shared]>
    Public Class BasicApplyDiagnosticAnalyzerAttributeFix
        Inherits ApplyDiagnosticAnalyzerAttributeFix

        Protected Overrides Function ParseExpression(expression As String) As SyntaxNode
            Return SyntaxFactory.ParseExpression(expression).WithAdditionalAnnotations(Simplifier.Annotation)
        End Function
    End Class
End Namespace
