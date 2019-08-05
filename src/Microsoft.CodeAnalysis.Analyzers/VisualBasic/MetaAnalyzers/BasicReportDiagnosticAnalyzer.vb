' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicReportDiagnosticAnalyzer
        Inherits ReportDiagnosticAnalyzer(Of ClassBlockSyntax, InvocationExpressionSyntax, IdentifierNameSyntax, VariableDeclaratorSyntax)

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

            Protected Overrides Function GetPropertyGetterBlockSyntax(declaringSyntaxRefNode As SyntaxNode) As SyntaxNode
                Select Case declaringSyntaxRefNode.Kind
                    Case SyntaxKind.GetAccessorBlock
                        Return declaringSyntaxRefNode

                    Case SyntaxKind.GetAccessorStatement
                        Return declaringSyntaxRefNode.Parent

                    Case Else
                        Return Nothing
                End Select
            End Function
        End Class
    End Class
End Namespace

