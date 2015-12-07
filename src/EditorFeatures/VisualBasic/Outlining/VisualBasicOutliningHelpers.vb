' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Module VisualBasicOutliningHelpers
        Public Const Ellipsis = "..."
        Public Const MaxXmlDocCommentBannerLength = 120

        Private Function GetCommentBannerText(comment As SyntaxTrivia) As String
            Return "' " & comment.ToString().Substring(1).Trim() & " " & Ellipsis
        End Function

        Private Function CreateCommentsRegion(startComment As SyntaxTrivia, endComment As SyntaxTrivia) As OutliningSpan
            Return CreateRegion(
                TextSpan.FromBounds(startComment.SpanStart, endComment.Span.End),
                GetCommentBannerText(startComment),
                autoCollapse:=True)
        End Function

        ' For testing purposes
        Friend Function CreateCommentsRegions(triviaList As SyntaxTriviaList) As IEnumerable(Of OutliningSpan)
            Dim spans = New List(Of OutliningSpan)
            CollectCommentsRegions(triviaList, spans)
            Return spans
        End Function

        Friend Sub CollectCommentsRegions(triviaList As SyntaxTriviaList, spans As List(Of OutliningSpan))
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

        Friend Sub CollectCommentsRegions(node As SyntaxNode, spans As List(Of OutliningSpan))
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim triviaList = node.GetLeadingTrivia()

            CollectCommentsRegions(triviaList, spans)
        End Sub

        Friend Function CreateRegion(textSpan As TextSpan, bannerText As String, autoCollapse As Boolean, Optional isDefaultCollapsed As Boolean = False) As OutliningSpan
            Return New OutliningSpan(textSpan, bannerText, autoCollapse, isDefaultCollapsed)
        End Function

        Friend Function CreateRegionFromBlock(node As SyntaxNode, bannerText As String, autoCollapse As Boolean) As OutliningSpan
            Return CreateRegion(node.Span, bannerText, autoCollapse)
        End Function

        Friend Function CreateRegion(syntaxList As IEnumerable(Of SyntaxNode), bannerText As String, autoCollapse As Boolean) As OutliningSpan
            If syntaxList.IsEmpty() Then
                Return Nothing
            End If

            Dim startPos = syntaxList.First().SpanStart
            Dim endPos = syntaxList.Last().Span.End
            Return CreateRegion(TextSpan.FromBounds(startPos, endPos), bannerText, autoCollapse)
        End Function
    End Module
End Namespace
