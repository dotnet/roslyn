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

        Protected Overloads Overrides Sub AddHighlights(node As SyntaxNode, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            If node.IsIncorrectContinueStatement(SyntaxKind.ContinueDoStatement) Then
                Return
            End If

            If node.IsIncorrectExitStatement(SyntaxKind.ExitDoStatement) Then
                Return
            End If

            Dim doLoop = node.GetAncestor(Of DoLoopBlockSyntax)()
            If doLoop Is Nothing Then
                Return
            End If

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
        End Sub
    End Class
End Namespace
