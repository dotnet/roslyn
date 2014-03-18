' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Usage
    <DiagnosticAnalyzer>
    <ExportDiagnosticAnalyzer(CA2200DiagnosticAnalyzer.RuleId, LanguageNames.VisualBasic)>
    Public Class BasicCA2200DiagnosticAnalyzer
        Inherits CA2200DiagnosticAnalyzer
        Implements ISyntaxNodeAnalyzer(Of SyntaxKind)

        Private Shared ReadOnly _kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(SyntaxKind.ThrowStatement)

        Public ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).SyntaxKindsOfInterest
            Get
                Return _kindsOfInterest
            End Get
        End Property

        Public Sub AnalyzeNode(node As SyntaxNode, semanticModel As SemanticModel, addDiagnostic As Action(Of Diagnostic), cancellationToken As CancellationToken) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).AnalyzeNode
            Dim throwStatement = DirectCast(node, ThrowStatementSyntax)
            Dim throwExpression = throwStatement.Expression
            If throwExpression Is Nothing Then
                Return
            End If

            Dim local = TryCast(semanticModel.GetSymbolInfo(throwExpression).Symbol, ILocalSymbol)
            If local Is Nothing OrElse local.Locations.Length = 0 Then
                Return
            End If

            ' if (local.LocalKind) TODO: expose LocalKind In the symbol model?

            While node IsNot Nothing
                Select Case node.VisualBasicKind
                    Case SyntaxKind.MultiLineFunctionLambdaExpression, SyntaxKind.MultiLineSubLambdaExpression,
                         SyntaxKind.SingleLineFunctionLambdaExpression, SyntaxKind.SingleLineSubLambdaExpression,
                         SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.ModuleBlock

                        Return

                    Case SyntaxKind.CatchStatement
                        Dim catchStatement = DirectCast(node, CatchStatementSyntax)
                        If CatchStatementIdentifierContainsSpan(catchStatement, local) Then
                            addDiagnostic(CreateDiagnostic(throwStatement))
                            Return
                        End If

                    Case SyntaxKind.CatchPart
                        Dim catchStatement = DirectCast(node, CatchPartSyntax).Begin
                        If CatchStatementIdentifierContainsSpan(catchStatement, local) Then
                            addDiagnostic(CreateDiagnostic(throwStatement))
                            Return
                        End If
                End Select

                node = node.Parent
            End While
        End Sub

        Private Shared Function CatchStatementIdentifierContainsSpan(catchStatement As CatchStatementSyntax, local As ILocalSymbol) As Boolean
            Return catchStatement.IdentifierName.Span.Contains(local.Locations(0).SourceSpan)
        End Function
    End Class
End Namespace
