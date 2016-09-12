' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Module VisualBasicOutliningHelpers
        Public Const Ellipsis = "..."
        Public Const SpaceEllipsis = " " & Ellipsis
        Public Const MaxXmlDocCommentBannerLength = 120

        Private Function GetNodeBannerText(node As SyntaxNode) As String
            Return node.ConvertToSingleLine().ToString() & SpaceEllipsis
        End Function

        Private Function GetCommentBannerText(comment As SyntaxTrivia) As String
            Return "' " & comment.ToString().Substring(1).Trim() & SpaceEllipsis
        End Function

        Private Function CreateCommentsRegion(startComment As SyntaxTrivia, endComment As SyntaxTrivia) As BlockSpan
            Return CreateRegion(
                TextSpan.FromBounds(startComment.SpanStart, endComment.Span.End),
                GetCommentBannerText(startComment),
                autoCollapse:=True)
        End Function

        ' For testing purposes
        Friend Function CreateCommentsRegions(triviaList As SyntaxTriviaList) As ImmutableArray(Of BlockSpan)
            Dim spans = ImmutableArray.CreateBuilder(Of BlockSpan)
            CollectCommentsRegions(triviaList, spans)
            Return spans.ToImmutable()
        End Function

        Friend Sub CollectCommentsRegions(triviaList As SyntaxTriviaList, spans As ImmutableArray(Of BlockSpan).Builder)
            If triviaList.Count > 0 Then
                Dim startComment As SyntaxTrivia? = Nothing
                Dim endComment As SyntaxTrivia? = Nothing

                ' Iterate through trivia and collect groups of contiguous single-line comments that are only separated by whitespace
                For Each trivia In triviaList
                    If trivia.Kind = SyntaxKind.CommentTrivia Then
                        startComment = If(startComment, trivia)
                        endComment = trivia
                    ElseIf trivia.Kind <> SyntaxKind.WhitespaceTrivia AndAlso
                        trivia.Kind <> SyntaxKind.EndOfLineTrivia AndAlso
                        trivia.Kind <> SyntaxKind.EndOfFileToken Then

                        If startComment IsNot Nothing Then
                            spans.Add(CreateCommentsRegion(startComment.Value, endComment.Value))
                            startComment = Nothing
                            endComment = Nothing
                        End If
                    End If
                Next

                ' Add any final span
                If startComment IsNot Nothing Then
                    spans.Add(CreateCommentsRegion(startComment.Value, endComment.Value))
                End If
            End If
        End Sub

        Friend Sub CollectCommentsRegions(node As SyntaxNode,
                                          spans As ImmutableArray(Of BlockSpan).Builder)
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim triviaList = node.GetLeadingTrivia()

            CollectCommentsRegions(triviaList, spans)
        End Sub

        Friend Function CreateRegion(span As TextSpan, bannerText As String, autoCollapse As Boolean, Optional isDefaultCollapsed As Boolean = False) As BlockSpan
            Return New BlockSpan(
                isCollapsible:=True,
                textSpan:=span,
                bannerText:=bannerText,
                autoCollapse:=autoCollapse,
                isDefaultCollapsed:=isDefaultCollapsed)
        End Function

        Friend Function CreateRegionFromBlock(blockNode As SyntaxNode, bannerText As String, autoCollapse As Boolean) As BlockSpan
            Return CreateRegion(blockNode.Span, bannerText, autoCollapse)
        End Function

        Friend Function CreateRegionFromBlock(blockNode As SyntaxNode, bannerNode As SyntaxNode, autoCollapse As Boolean) As BlockSpan
            Return CreateRegion(blockNode.Span, GetNodeBannerText(bannerNode), autoCollapse)
        End Function

        Friend Function CreateRegion(syntaxList As IEnumerable(Of SyntaxNode), bannerText As String, autoCollapse As Boolean) As BlockSpan
            If syntaxList.IsEmpty() Then
                Return Nothing
            End If

            Dim startPos = syntaxList.First().SpanStart
            Dim endPos = syntaxList.Last().Span.End
            Return CreateRegion(TextSpan.FromBounds(startPos, endPos), bannerText, autoCollapse)
        End Function
    End Module
End Namespace