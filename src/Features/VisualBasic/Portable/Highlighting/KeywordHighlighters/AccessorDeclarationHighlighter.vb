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
    Friend Class AccessorDeclarationHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
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
