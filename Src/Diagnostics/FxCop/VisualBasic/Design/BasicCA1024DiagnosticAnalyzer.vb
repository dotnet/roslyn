' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Design
Imports System.Collections.Immutable
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Design
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicCA1024DiagnosticAnalyzer
        Inherits CA1024DiagnosticAnalyzer

        Protected Overrides Function GetCodeBlockEndedAnalyzer() As CA1024CodeBlockEndedAnalyzer
            Return New CodeBlockEndedAnalyzer()
        End Function

        Private Class CodeBlockEndedAnalyzer
            Inherits CA1024CodeBlockEndedAnalyzer
            Implements ISyntaxNodeAnalyzer(Of SyntaxKind)

            Private Shared ReadOnly _kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(Of SyntaxKind)(SyntaxKind.InvocationExpression)
            Public ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).SyntaxKindsOfInterest
                Get
                    Return _kindsOfInterest
                End Get
            End Property

            Private Sub BasicAnalyzeNode(node As SyntaxNode, semanticModel As SemanticModel, addDiagnostic As Action(Of Diagnostic), options As AnalyzerOptions, cancellationToken As CancellationToken) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).AnalyzeNode
                AnalyzeNode(node, semanticModel, addDiagnostic, options, cancellationToken)
            End Sub

            Protected Overrides Function GetDiagnosticLocation(node As SyntaxNode) As Location
                Dim methodBlock = TryCast(node, MethodBlockSyntax)
                If methodBlock IsNot Nothing Then
                    Return methodBlock.Begin.Identifier.GetLocation()
                End If

                Dim methodStatement = TryCast(node, MethodStatementSyntax)
                If methodStatement IsNot Nothing Then
                    Return methodStatement.Identifier.GetLocation()
                End If

                Return Location.None
            End Function

        End Class
    End Class
End Namespace