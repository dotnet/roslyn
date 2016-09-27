' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class DocumentationCommentStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of DocumentationCommentTriviaSyntax)

        Private Shared Function GetBannerText(documentationComment As DocumentationCommentTriviaSyntax, cancellationToken As CancellationToken) As String
            ' TODO: Consider unifying code to extract text from an Xml Documentation Comment (https://github.com/dotnet/roslyn/issues/2290)
            Dim summaryElement = documentationComment.Content _
                .OfType(Of XmlElementSyntax)() _
                .FirstOrDefault(Function(e) e.StartTag.Name.ToString = "summary")

            Dim text As String
            If summaryElement IsNot Nothing Then
                Dim sb As New StringBuilder(summaryElement.Span.Length)
                sb.Append("''' <summary>")
                For Each node In summaryElement.ChildNodes()
                    If node.Kind() = SyntaxKind.XmlText Then
                        Dim textNode = DirectCast(node, XmlTextSyntax)
                        Dim textTokens As SyntaxTokenList = textNode.TextTokens
                        AppendTextTokens(sb, textTokens)
                    ElseIf node.Kind() = SyntaxKind.XmlEmptyElement Then
                        Dim elementNode = DirectCast(node, XmlEmptyElementSyntax)
                        For Each attribute In elementNode.Attributes
                            If TypeOf attribute Is XmlCrefAttributeSyntax Then
                                sb.Append(" ")
                                sb.Append(DirectCast(attribute, XmlCrefAttributeSyntax).Reference.ToString())
                            ElseIf TypeOf attribute Is XmlNameAttributeSyntax Then
                                sb.Append(" ")
                                sb.Append(DirectCast(attribute, XmlNameAttributeSyntax).Reference.ToString())
                            ElseIf TypeOf attribute Is XmlAttributeSyntax Then
                                AppendTextTokens(sb, DirectCast(DirectCast(attribute, XmlAttributeSyntax).Value, XmlStringSyntax).TextTokens)
                            Else
                                Debug.Assert(False, $"Unexpected XML syntax kind {attribute.Kind()}")
                            End If
                        Next
                    End If
                Next

                text = sb.ToString()
            Else
                ' If a summary element isn't found, use the first line of the XML doc comment.
                Dim span = documentationComment.Span
                Dim syntaxTree = documentationComment.SyntaxTree
                Dim line = syntaxTree.GetText(cancellationToken).Lines.GetLineFromPosition(span.Start)
                text = "''' " & line.ToString().Substring(span.Start - line.Start).Trim() & SpaceEllipsis
            End If

            If text.Length > MaxXmlDocCommentBannerLength Then
                text = text.Substring(0, MaxXmlDocCommentBannerLength) & SpaceEllipsis
            End If

            Return text
        End Function

        Private Shared Sub AppendTextTokens(sb As StringBuilder, textTokens As SyntaxTokenList)
            For Each token In textTokens.Where(Function(t) t.Kind = SyntaxKind.XmlTextLiteralToken)
                Dim s = token.ToString().Trim()
                If s.Length <> 0 Then
                    sb.Append(" ")
                    sb.Append(s)
                End If
            Next
        End Sub

        Protected Overrides Sub CollectBlockSpans(documentationComment As DocumentationCommentTriviaSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  cancellationToken As CancellationToken)
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

            spans.Add(CreateRegion(
                fullSpan, GetBannerText(documentationComment, cancellationToken),
                autoCollapse:=True, type:=BlockTypes.Nonstructural, isCollapsible:=True))
        End Sub
    End Class
End Namespace