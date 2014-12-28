' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Diagnostics.Analyzers

Namespace Roslyn.Diagnostics.CodeFixes.VisualBasic
    <ExportCodeFixProvider(RoslynDiagnosticIds.MissingDiagnosticAnalyzerAttributeRuleId, LanguageNames.VisualBasic), [Shared]>
    Public Class BasicApplyDiagnosticAnalyzerAttributeFix
        Inherits ApplyDiagnosticAnalyzerAttributeFix

        Protected Overrides Function ParseExpression(expression As String) As SyntaxNode
            Return SyntaxFactory.ParseExpression(expression).WithAdditionalAnnotations(Simplifier.Annotation)
        End Function
    End Class
End Namespace
