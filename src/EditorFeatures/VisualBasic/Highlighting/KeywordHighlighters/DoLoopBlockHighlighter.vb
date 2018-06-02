' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class DoLoopBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        Protected Overloads Overrides Iterator Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested OrElse
               node.IsIncorrectContinueStatement(SyntaxKind.ContinueDoStatement) OrElse
               node.IsIncorrectExitStatement(SyntaxKind.ExitDoStatement) Then
                Return
            End If

            Dim doLoop = node.GetAncestor(Of DoLoopBlockSyntax)()
            If doLoop Is Nothing Then
                Return
            End If


            With doLoop.DoStatement
                Yield If(.WhileOrUntilClause Is Nothing, .DoKeyword.Span, TextSpan.FromBounds(.DoKeyword.SpanStart, .WhileOrUntilClause.WhileOrUntilKeyword.Span.End))
            End With

            For Each highlight In doLoop.GetRelatedStatementHighlights(blockKind:=SyntaxKind.DoKeyword)
                If cancellationToken.IsCancellationRequested Then
                    Return
                End If
                Yield highlight
            Next

            With doLoop.LoopStatement
                Yield If(.WhileOrUntilClause Is Nothing, .LoopKeyword.Span, TextSpan.FromBounds(.LoopKeyword.SpanStart, .WhileOrUntilClause.WhileOrUntilKeyword.Span.End))
            End With
        End Function
    End Class
End Namespace
