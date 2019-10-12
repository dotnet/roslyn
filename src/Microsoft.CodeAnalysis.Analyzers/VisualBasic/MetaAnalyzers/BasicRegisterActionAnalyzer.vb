' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Analyzer.Utilities.Extensions
Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicRegisterActionAnalyzer
        Inherits RegisterActionAnalyzer(Of ClassBlockSyntax, InvocationExpressionSyntax, ArgumentSyntax, SyntaxKind)

        Private Const BasicSyntaxKindFullName As String = "Microsoft.CodeAnalysis.VisualBasic.SyntaxKind"
        Private Const CSharpSyntaxKindFullName As String = "Microsoft.CodeAnalysis.CSharp.SyntaxKind"

        Protected Overrides Function GetCodeBlockAnalyzer(compilation As Compilation,
                                                 analysisContext As INamedTypeSymbol,
                                                 compilationStartAnalysisContext As INamedTypeSymbol,
                                                 codeBlockStartAnalysisContext As INamedTypeSymbol,
                                                 operationBlockStartAnalysisContext As INamedTypeSymbol,
                                                 symbolKind As INamedTypeSymbol) As RegisterActionCodeBlockAnalyzer
            Dim basicSyntaxKind = compilation.GetOrCreateTypeByMetadataName(BasicSyntaxKindFullName)
            Dim csharpSyntaxKind = compilation.GetOrCreateTypeByMetadataName(CSharpSyntaxKindFullName)
            Return New BasicRegisterActionCodeBlockAnalyzer(basicSyntaxKind, csharpSyntaxKind, analysisContext, compilationStartAnalysisContext, codeBlockStartAnalysisContext,
                                                            operationBlockStartAnalysisContext, symbolKind)
        End Function

        Private NotInheritable Class BasicRegisterActionCodeBlockAnalyzer
            Inherits RegisterActionCodeBlockAnalyzer

            Private ReadOnly _csharpSyntaxKind As ITypeSymbol
            Private ReadOnly _basicSyntaxKind As ITypeSymbol

            Public Sub New(basicSyntaxKind As INamedTypeSymbol,
                           csharpSyntaxKind As INamedTypeSymbol,
                           analysisContext As INamedTypeSymbol,
                           compilationStartAnalysisContext As INamedTypeSymbol,
                           codeBlockStartAnalysisContext As INamedTypeSymbol,
                           operationBlockStartAnalysisContext As INamedTypeSymbol,
                           symbolKind As INamedTypeSymbol)
                MyBase.New(analysisContext, compilationStartAnalysisContext, codeBlockStartAnalysisContext, operationBlockStartAnalysisContext, symbolKind)

                Me._basicSyntaxKind = basicSyntaxKind
                Me._csharpSyntaxKind = csharpSyntaxKind
            End Sub

            Protected Overrides ReadOnly Property InvocationExpressionKind As SyntaxKind
                Get
                    Return SyntaxKind.InvocationExpression
                End Get
            End Property

            Protected Overrides ReadOnly Property ArgumentSyntaxKind As SyntaxKind
                Get
                    Return SyntaxKind.SimpleArgument
                End Get
            End Property

            Protected Overrides ReadOnly Property ParameterSyntaxKind As SyntaxKind
                Get
                    Return SyntaxKind.Parameter
                End Get
            End Property

            Protected Overrides Function GetArgumentExpressions(invocation As InvocationExpressionSyntax) As IEnumerable(Of SyntaxNode)
                If invocation.ArgumentList IsNot Nothing Then
                    Return invocation.ArgumentList.Arguments.Select(Function(a) a.GetExpression)
                End If

                Return Nothing
            End Function

            Protected Overrides Function GetArgumentExpression(argument As ArgumentSyntax) As SyntaxNode
                Return argument.GetExpression
            End Function

            Protected Overrides Function GetInvocationExpression(invocation As InvocationExpressionSyntax) As SyntaxNode
                Return invocation.Expression
            End Function

            Protected Overrides Function GetInvocationReceiver(invocation As InvocationExpressionSyntax) As SyntaxNode
                Return TryCast(invocation.Expression, MemberAccessExpressionSyntax)?.Expression
            End Function

            Protected Overrides Function IsSyntaxKind(type As ITypeSymbol) As Boolean
                Return (Me._basicSyntaxKind IsNot Nothing AndAlso type.Equals(Me._basicSyntaxKind)) OrElse
                    (Me._csharpSyntaxKind IsNot Nothing AndAlso type.Equals(Me._csharpSyntaxKind))
            End Function
        End Class
    End Class
End Namespace
