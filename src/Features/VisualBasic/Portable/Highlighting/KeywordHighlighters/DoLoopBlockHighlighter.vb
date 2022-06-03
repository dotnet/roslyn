' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Highlighting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic), [Shared]>
    Friend Class DoLoopBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
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
