' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class DoLoopBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If node.IsIncorrectContinueStatement(SyntaxKind.ContinueDoStatement) Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            If node.IsIncorrectExitStatement(SyntaxKind.ExitDoStatement) Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            Dim doLoop = node.GetAncestor(Of DoLoopBlockSyntax)()
            If doLoop Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            Dim highlights As New List(Of TextSpan)

            With doLoop.DoStatement
                If .WhileOrUntilClause IsNot Nothing Then
                    highlights.Add(TextSpan.FromBounds(.DoKeyword.SpanStart, .WhileOrUntilClause.WhileOrUntilKeyword.Span.End))
                Else
                    highlights.Add(.DoKeyword.Span)
                End If
            End With

            highlights.AddRange(
                doLoop.GetRelatedStatementHighlights(
                    blockKind:=SyntaxKind.DoKeyword))

            With doLoop.LoopStatement
                If .WhileOrUntilClause IsNot Nothing Then
                    highlights.Add(TextSpan.FromBounds(.LoopKeyword.SpanStart, .WhileOrUntilClause.WhileOrUntilKeyword.Span.End))
                Else
                    highlights.Add(.LoopKeyword.Span)
                End If
            End With

            Return highlights
        End Function
    End Class
End Namespace
