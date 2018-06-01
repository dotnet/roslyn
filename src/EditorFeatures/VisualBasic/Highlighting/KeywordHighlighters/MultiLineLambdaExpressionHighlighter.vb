' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class MultiLineLambdaExpressionHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        Protected Overloads Overrides Iterator Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return
            Dim lambdaExpression = node.GetAncestor(Of MultiLineLambdaExpressionSyntax)()
            If lambdaExpression Is Nothing Then Return

            With lambdaExpression
                Dim isAsync = False
                Dim isIterator = False

                With .SubOrFunctionHeader
                    isAsync = .Modifiers.Any(SyntaxKind.AsyncKeyword)
                    isIterator = .Modifiers.Any(SyntaxKind.IteratorKeyword)

                    Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .DeclarationKeyword)
                    Yield TextSpan.FromBounds(firstKeyword.SpanStart, .DeclarationKeyword.Span.End)
                End With

                For Each highlight In .GetRelatedStatementHighlights(blockKind:= .SubOrFunctionHeader.DeclarationKeyword.Kind, checkReturns:=True)
                    If cancellationToken.IsCancellationRequested Then Return
                    Yield highlight
                Next

                If isIterator Then
                    For Each highlight In .GetRelatedYieldStatementHighlights()
                        If cancellationToken.IsCancellationRequested Then Return
                        Yield highlight
                    Next
                End If

                If isAsync Then
                    For Each highlight In HighlightRelatedAwaits(lambdaExpression, cancellationToken)
                        If cancellationToken.IsCancellationRequested Then Return
                        Yield highlight
                    Next
                End If

                Yield .EndSubOrFunctionStatement.Span
            End With

        End Function
    End Class
End Namespace
