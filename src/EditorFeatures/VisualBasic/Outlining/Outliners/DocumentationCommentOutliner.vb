' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class DocumentationCommentOutliner
        Inherits AbstractSyntaxNodeOutliner(Of DocumentationCommentTriviaSyntax)

        Private Shared Function GetBannerText(documentationComment As DocumentationCommentTriviaSyntax, cancellationToken As CancellationToken) As String
            Dim summaryElement = documentationComment.Content.OfType(Of XmlElementSyntax)() _
                                    .FirstOrDefault(Function(e) e.StartTag.Name.ToString = "summary")

            Dim text As String
            If summaryElement IsNot Nothing Then
                Dim summaryText = From nodes In summaryElement.ChildNodesAndTokens().Where(Function(node) node.Kind() = SyntaxKind.XmlText).Select(Function(xmlText) DirectCast(xmlText.AsNode(), XmlTextSyntax))
                                  From token In nodes.TextTokens.Where(Function(t) t.Kind = SyntaxKind.XmlTextLiteralToken)
                                  Let s = token.ToString().Trim()
                                  Where (s.Length > 0)
                                  Select s

                text = "''' <summary> " & String.Join(" ", summaryText)
            Else
                ' If a summary element isn't found, use the first line of the XML doc comment.
                Dim span = documentationComment.Span
                Dim syntaxTree = documentationComment.SyntaxTree
                Dim line = syntaxTree.GetText(cancellationToken).Lines.GetLineFromPosition(span.Start)
                text = "''' " & line.ToString().Substring(span.Start - line.Start).Trim() & " " + Ellipsis
            End If

            If text.Length > MaxXmlDocCommentBannerLength Then
                text = text.Substring(0, MaxXmlDocCommentBannerLength) & " " & Ellipsis
            End If

            Return text
        End Function

        Protected Overrides Sub CollectOutliningSpans(documentationComment As DocumentationCommentTriviaSyntax, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            Dim firstCommentToken = documentationComment.ChildNodesAndTokens().FirstOrNullable()
            Dim lastCommentToken = documentationComment.ChildNodesAndTokens().LastOrNullable()
            If firstCommentToken Is Nothing Then
                Return
            End If

            ' TODO: Need to redo this when DocumentationCommentTrivia.SpanStart points to the start of the exterior trivia.
            Dim startPos = firstCommentToken.Value.FullSpan.Start

            ' The trailing newline is included in DocumentationCommentTrivia, so we need to strip it.
            Dim endPos = lastCommentToken.Value.SpanStart + lastCommentToken.Value.ToString().TrimEnd().Length

            Dim fullSpan = TextSpan.FromBounds(startPos, endPos)

            spans.Add(VisualBasicOutliningHelpers.CreateRegion(
                            fullSpan,
                            GetBannerText(documentationComment, cancellationToken),
                            autoCollapse:=True))
        End Sub
    End Class
End Namespace
