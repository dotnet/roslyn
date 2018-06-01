' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class MethodDeclarationHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        Protected Overloads Overrides Iterator Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return
            Dim methodBlock = node.GetAncestor(Of MethodBlockBaseSyntax)()
            If methodBlock Is Nothing OrElse TypeOf methodBlock.BlockStatement IsNot MethodStatementSyntax Then Return

            With methodBlock
                Dim isAsync = False
                Dim isIterator = False

                With DirectCast(.BlockStatement, MethodStatementSyntax)
                    isAsync = .Modifiers.Any(SyntaxKind.AsyncKeyword)
                    isIterator = .Modifiers.Any(SyntaxKind.IteratorKeyword)

                    Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .DeclarationKeyword)
                    Yield TextSpan.FromBounds(firstKeyword.SpanStart, .DeclarationKeyword.Span.End)

                    If .HandlesClause IsNot Nothing Then Yield .HandlesClause.HandlesKeyword.Span
                    If .ImplementsClause IsNot Nothing Then Yield .ImplementsClause.ImplementsKeyword.Span

                End With

                For Each highlight In .GetRelatedStatementHighlights(blockKind:= .BlockStatement.DeclarationKeyword.Kind, checkReturns:=True)
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
                    For Each highlight In HighlightRelatedAwaits(methodBlock, cancellationToken)
                        If cancellationToken.IsCancellationRequested Then Return
                        Yield highlight
                    Next
                End If
                Yield .EndBlockStatement.Span
            End With

        End Function
    End Class
End Namespace
