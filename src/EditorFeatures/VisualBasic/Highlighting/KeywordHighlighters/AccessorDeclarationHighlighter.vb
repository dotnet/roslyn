' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class AccessorDeclarationHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        Protected Overloads Overrides Iterator Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return

            Dim methodBlock = node.GetAncestor(Of MethodBlockBaseSyntax)()
            If methodBlock Is Nothing OrElse Not TypeOf methodBlock.BlockStatement Is AccessorStatementSyntax Then Return
            With methodBlock
                Dim isIterator = False

                If TypeOf methodBlock.Parent Is PropertyBlockSyntax Then
                    isIterator = DirectCast(methodBlock.Parent, PropertyBlockSyntax).PropertyStatement.Modifiers.Any(SyntaxKind.IteratorKeyword)
                End If

                With DirectCast(.BlockStatement, AccessorStatementSyntax)
                    Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .DeclarationKeyword)
                    Yield TextSpan.FromBounds(firstKeyword.SpanStart, .DeclarationKeyword.Span.End)
                End With

                Dim blockKind = If(node.HasAncestor(Of PropertyBlockSyntax), SyntaxKind.PropertyKeyword, SyntaxKind.None)
                For Each highlight In .GetRelatedStatementHighlights(blockKind, checkReturns:=True)
                    If cancellationToken.IsCancellationRequested Then Return
                    Yield highlight
                Next

                If isIterator Then
                    For Each highlight In .GetRelatedYieldStatementHighlights()
                        If cancellationToken.IsCancellationRequested Then Return
                        Yield highlight
                    Next
                End If
                Yield .EndBlockStatement.Span
            End With

        End Function
    End Class
End Namespace
