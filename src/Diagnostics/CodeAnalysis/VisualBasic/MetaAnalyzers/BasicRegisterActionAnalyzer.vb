' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicRegisterActionAnalyzer
        Inherits RegisterActionAnalyzer(Of ClassBlockSyntax, InvocationExpressionSyntax, SyntaxKind)

        Private Shared ReadOnly BasicSyntaxKindFullName As String = GetType(SyntaxKind).FullName
        Private Shared ReadOnly CSharpSyntaxKindFullName As String = "Microsoft.CodeAnalysis.CSharp.SyntaxKind"

        Protected Overrides Function GetAnalyzer(compilation As Compilation,
                                                 analysisContext As INamedTypeSymbol,
                                                 compilationStartAnalysisContext As INamedTypeSymbol,
                                                 codeBlockStartAnalysisContext As INamedTypeSymbol,
                                                 symbolKind As INamedTypeSymbol,
                                                 diagnosticAnalyzer As INamedTypeSymbol,
                                                 diagnosticAnalyzerAttribute As INamedTypeSymbol) As RegisterActionCompilationAnalyzer
            Dim basicSyntaxKind = compilation.GetTypeByMetadataName(BasicSyntaxKindFullName)
            Dim csharpSyntaxKind = compilation.GetTypeByMetadataName(CSharpSyntaxKindFullName)
            Return New BasicRegisterActionCompilationAnalyzer(basicSyntaxKind, csharpSyntaxKind, analysisContext, compilationStartAnalysisContext, codeBlockStartAnalysisContext, symbolKind, diagnosticAnalyzer, diagnosticAnalyzerAttribute)
        End Function

        Private NotInheritable Class BasicRegisterActionCompilationAnalyzer
            Inherits RegisterActionCompilationAnalyzer

            Private ReadOnly csharpSyntaxKind As ITypeSymbol
            Private ReadOnly basicSyntaxKind As ITypeSymbol

            Public Sub New(basicSyntaxKind As INamedTypeSymbol,
                           csharpSyntaxKind As INamedTypeSymbol,
                           analysisContext As INamedTypeSymbol,
                           compilationStartAnalysisContext As INamedTypeSymbol,
                           codeBlockStartAnalysisContext As INamedTypeSymbol,
                           symbolKind As INamedTypeSymbol,
                           diagnosticAnalyzer As INamedTypeSymbol,
                           diagnosticAnalyzerAttribute As INamedTypeSymbol)
                MyBase.New(analysisContext, compilationStartAnalysisContext, codeBlockStartAnalysisContext, symbolKind, diagnosticAnalyzer, diagnosticAnalyzerAttribute)

                Me.basicSyntaxKind = basicSyntaxKind
                Me.csharpSyntaxKind = csharpSyntaxKind
            End Sub

            Protected Overrides Function GetArgumentExpressions(invocation As InvocationExpressionSyntax) As IEnumerable(Of SyntaxNode)
                If invocation.ArgumentList IsNot Nothing Then
                    Return invocation.ArgumentList.Arguments.Select(Function(a) a.GetExpression)
                End If

                Return Nothing
            End Function

            Protected Overrides Function GetInvocationExpression(invocation As InvocationExpressionSyntax) As SyntaxNode
                Return invocation.Expression
            End Function

            Protected Overrides Function IsSyntaxKind(type As ITypeSymbol) As Boolean
                Return (Me.basicSyntaxKind IsNot Nothing AndAlso type.Equals(Me.basicSyntaxKind)) OrElse
                    (Me.csharpSyntaxKind IsNot Nothing AndAlso type.Equals(Me.csharpSyntaxKind))
            End Function
        End Class
    End Class
End Namespace
