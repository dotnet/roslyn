' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class AccessorDeclarationHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Sub AddHighlights(node As SyntaxNode, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            Dim methodBlock = node.GetAncestor(Of MethodBlockBaseSyntax)()
            If methodBlock Is Nothing OrElse Not TypeOf methodBlock.BlockStatement Is AccessorStatementSyntax Then
                Return
            End If

            With methodBlock
                Dim isIterator = False

                If TypeOf methodBlock.Parent Is PropertyBlockSyntax Then
                    With DirectCast(methodBlock.Parent, PropertyBlockSyntax)
                        isIterator = .PropertyStatement.Modifiers.Any(SyntaxKind.IteratorKeyword)
                    End With
                End If

                With DirectCast(.BlockStatement, AccessorStatementSyntax)
                    Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .DeclarationKeyword)
                    highlights.Add(TextSpan.FromBounds(firstKeyword.SpanStart, .DeclarationKeyword.Span.End))
                End With

                Dim blockKind = If(node.HasAncestor(Of PropertyBlockSyntax)(),
                                   SyntaxKind.PropertyKeyword,
                                   SyntaxKind.None)

                highlights.AddRange(
                    .GetRelatedStatementHighlights(
                        blockKind,
                        checkReturns:=True))

                If isIterator Then
                    highlights.AddRange(.GetRelatedYieldStatementHighlights())
                End If

                highlights.Add(.EndBlockStatement.Span)
            End With
        End Sub
    End Class
End Namespace
