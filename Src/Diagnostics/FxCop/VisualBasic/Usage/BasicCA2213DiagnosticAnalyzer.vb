' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Usage
    <DiagnosticAnalyzer>
    <ExportDiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicCA2213DiagnosticAnalyzer
        Inherits CA2213DiagnosticAnalyzer

        Protected Overrides Function GetAnalyzer(disposableType As INamedTypeSymbol) As AbstractAnalyzer
            Return New Analyzer(disposableType)
        End Function

        Private NotInheritable Class Analyzer
            Inherits AbstractAnalyzer
            Implements ISyntaxNodeAnalyzer(Of SyntaxKind)

            Dim _kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.UsingStatement)

            Public Sub New(disposableType As INamedTypeSymbol)
                MyBase.New(disposableType)
            End Sub

            Public ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).SyntaxKindsOfInterest
                Get
                    Return _kindsOfInterest
                End Get
            End Property

            Public Sub AnalyzeNode(node As SyntaxNode, semanticModel As SemanticModel, addDiagnostic As Action(Of Diagnostic), options As AnalyzerOptions, cancellationToken As CancellationToken) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).AnalyzeNode
                Select Case node.VisualBasicKind
                    Case SyntaxKind.SimpleMemberAccessExpression
                        ' NOTE: This cannot be optimized based on memberAccess.Name because a given method
                        ' may be an explicit interface implementation of IDisposable.Dispose.
                        Dim memberAccess = DirectCast(node, MemberAccessExpressionSyntax)
                        Dim methodSymbol = TryCast(semanticModel.GetSymbolInfo(memberAccess.Name).Symbol, IMethodSymbol)
                        If methodSymbol IsNot Nothing AndAlso
                            (methodSymbol.MetadataName = Dispose OrElse methodSymbol.ExplicitInterfaceImplementations.Any(Function(m) m.MetadataName = Dispose)) Then
                            Dim exp = RemoveParentheses(memberAccess.Expression)
                            Dim fieldSymbol = TryCast(semanticModel.GetSymbolInfo(exp).Symbol, IFieldSymbol)
                            If fieldSymbol IsNot Nothing Then
                                NoteFieldDisposed(fieldSymbol)
                            End If
                        End If

                    Case SyntaxKind.UsingStatement
                        Dim usingStatementExpression = RemoveParentheses(DirectCast(node, UsingStatementSyntax).Expression)
                        If usingStatementExpression IsNot Nothing Then
                            Dim fieldSymbol = TryCast(semanticModel.GetSymbolInfo(usingStatementExpression).Symbol, IFieldSymbol)
                            If fieldSymbol IsNot Nothing Then
                                NoteFieldDisposed(fieldSymbol)
                            End If
                        End If
                End Select
            End Sub

            Private Function RemoveParentheses(exp As ExpressionSyntax) As ExpressionSyntax
                Dim syntax = TryCast(exp, ParenthesizedExpressionSyntax)
                If syntax IsNot Nothing Then
                    Return RemoveParentheses(syntax.Expression)
                End If

                Return exp
            End Function
        End Class
    End Class
End Namespace