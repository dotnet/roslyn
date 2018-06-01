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
            If cancellationToken.IsCancellationRequested Then Return
            If node.IsIncorrectContinueStatement(SyntaxKind.ContinueDoStatement) Then Return
            If node.IsIncorrectExitStatement(SyntaxKind.ExitDoStatement) Then Return
            Dim doLoop = node.GetAncestor(Of DoLoopBlockSyntax)()
            If doLoop Is Nothing Then Return


            With doLoop.DoStatement
                Yield If(.WhileOrUntilClause Is Nothing, .DoKeyword.Span, TextSpan.FromBounds(.DoKeyword.SpanStart, .WhileOrUntilClause.WhileOrUntilKeyword.Span.End))
            End With

            For Each highlight In doLoop.GetRelatedStatementHighlights(blockKind:=SyntaxKind.DoKeyword)
                If cancellationToken.IsCancellationRequested Then Return
                Yield highlight
            Next

            With doLoop.LoopStatement
                Yield If(.WhileOrUntilClause Is Nothing, .LoopKeyword.Span, TextSpan.FromBounds(.LoopKeyword.SpanStart, .WhileOrUntilClause.WhileOrUntilKeyword.Span.End))
            End With
        End Function
    End Class
End Namespace
