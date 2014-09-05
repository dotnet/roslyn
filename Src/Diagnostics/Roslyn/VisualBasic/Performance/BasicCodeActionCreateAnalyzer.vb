' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Roslyn.Diagnostics.Analyzers.VisualBasic
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicCodeActionCreateAnalyzer
        Inherits CodeActionCreateAnalyzer

        Protected Overrides Function GetCodeBlockStartedAnalyzer(symbols As ImmutableHashSet(Of ISymbol)) As AbstractCodeBlockStartedAnalyzer
            Return New CodeBlockStartedAnalyzer(symbols)
        End Function

        Private NotInheritable Class CodeBlockStartedAnalyzer
            Inherits AbstractCodeBlockStartedAnalyzer

            Public Sub New(symbols As ImmutableHashSet(Of ISymbol))
                MyBase.New(symbols)
            End Sub

            Protected Overrides Function GetSyntaxAnalyzer(symbols As ImmutableHashSet(Of ISymbol)) As AbstractSyntaxAnalyzer
                Return New SyntaxAnalyzer(symbols)
            End Function
        End Class

        Private NotInheritable Class SyntaxAnalyzer
            Inherits AbstractSyntaxAnalyzer
            Implements ISyntaxNodeAnalyzer(Of SyntaxKind)

            Public Sub New(symbols As ImmutableHashSet(Of ISymbol))
                MyBase.New(symbols)
            End Sub

            Public ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).SyntaxKindsOfInterest
                Get
                    Return ImmutableArray.Create(SyntaxKind.InvocationExpression)
                End Get
            End Property

            Public Sub AnalyzeNode(node As SyntaxNode, semanticModel As SemanticModel, addDiagnostic As Action(Of Diagnostic), options As AnalyzerOptions, cancellationToken As CancellationToken) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).AnalyzeNode
                Dim invocation = TryCast(node, InvocationExpressionSyntax)
                If invocation Is Nothing Then
                    Return
                End If

                AnalyzeInvocationExpression(invocation.Expression, semanticModel, addDiagnostic, cancellationToken)
            End Sub
        End Class
    End Class
End Namespace
