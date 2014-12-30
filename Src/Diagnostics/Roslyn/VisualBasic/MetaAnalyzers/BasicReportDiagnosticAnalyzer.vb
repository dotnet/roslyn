' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Roslyn.Diagnostics.Analyzers.MetaAnalyzers.VisualBasic
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicReportDiagnosticAnalyzer
        Inherits ReportDiagnosticAnalyzer(Of ClassBlockSyntax, InvocationExpressionSyntax, IdentifierNameSyntax)

        Protected Overrides Function GetAnalyzer(contextTypes As ImmutableHashSet(Of INamedTypeSymbol),
                                                 diagnosticType As INamedTypeSymbol,
                                                 diagnosticDescriptorType As INamedTypeSymbol,
                                                 diagnosticAnalyzer As INamedTypeSymbol,
                                                 diagnosticAnalyzerAttribute As INamedTypeSymbol) As ReportDiagnosticCompilationAnalyzer
            Return New BasicReportDiagnosticCompilationAnalyzer(contextTypes, diagnosticType, diagnosticDescriptorType, diagnosticAnalyzer, diagnosticAnalyzerAttribute)
        End Function

        Private NotInheritable Class BasicReportDiagnosticCompilationAnalyzer
            Inherits ReportDiagnosticCompilationAnalyzer

            Public Sub New(contextTypes As ImmutableHashSet(Of INamedTypeSymbol),
                           diagnosticType As INamedTypeSymbol,
                           diagnosticDescriptorType As INamedTypeSymbol,
                           diagnosticAnalyzer As INamedTypeSymbol,
                           diagnosticAnalyzerAttribute As INamedTypeSymbol)
                MyBase.New(contextTypes, diagnosticType, diagnosticDescriptorType, diagnosticAnalyzer, diagnosticAnalyzerAttribute)
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

