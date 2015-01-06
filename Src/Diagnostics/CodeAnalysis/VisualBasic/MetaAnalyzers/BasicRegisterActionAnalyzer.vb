' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.VisualBasic
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicRegisterActionAnalyzer
        Inherits RegisterActionAnalyzer(Of ClassBlockSyntax, InvocationExpressionSyntax, SyntaxKind)

        Protected Overrides Function GetAnalyzer(analysisContext As INamedTypeSymbol,
                                                 compilationStartAnalysisContext As INamedTypeSymbol,
                                                 codeBlockStartAnalysisContext As INamedTypeSymbol,
                                                 symbolKind As INamedTypeSymbol,
                                                 diagnosticAnalyzer As INamedTypeSymbol,
                                                 diagnosticAnalyzerAttribute As INamedTypeSymbol) As RegisterActionCompilationAnalyzer
            Return New BasicRegisterActionCompilationAnalyzer(analysisContext, compilationStartAnalysisContext, codeBlockStartAnalysisContext, symbolKind, diagnosticAnalyzer, diagnosticAnalyzerAttribute)
        End Function

        Private NotInheritable Class BasicRegisterActionCompilationAnalyzer
            Inherits RegisterActionCompilationAnalyzer

            Public Sub New(analysisContext As INamedTypeSymbol,
                           compilationStartAnalysisContext As INamedTypeSymbol,
                           codeBlockStartAnalysisContext As INamedTypeSymbol,
                           symbolKind As INamedTypeSymbol,
                           diagnosticAnalyzer As INamedTypeSymbol,
                           diagnosticAnalyzerAttribute As INamedTypeSymbol)
                MyBase.New(analysisContext, compilationStartAnalysisContext, codeBlockStartAnalysisContext, symbolKind, diagnosticAnalyzer, diagnosticAnalyzerAttribute)
            End Sub

            Protected Overrides Function GetArgumentExpressions(invocation As InvocationExpressionSyntax) As IEnumerable(Of SyntaxNode)
                If invocation.ArgumentList IsNot Nothing Then
                    Return invocation.ArgumentList.Arguments.Select(Function(a) a.GetExpression)
                End If

                Return Nothing
            End Function
        End Class
    End Class
End Namespace
