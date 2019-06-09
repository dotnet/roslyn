' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

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

        Private Function CreateCommentsRegion(startComment As SyntaxTrivia,
                                              endComment As SyntaxTrivia) As BlockSpan?
            Dim span = TextSpan.FromBounds(startComment.SpanStart, endComment.Span.End)
            Return CreateBlockSpan(
                span, span,
                GetCommentBannerText(startComment),
                autoCollapse:=True,
                type:=BlockTypes.Comment,
                isCollapsible:=True,
                isDefaultCollapsed:=False)
        End Function

        ' For testing purposes
        Friend Function CreateCommentsRegions(triviaList As SyntaxTriviaList) As ImmutableArray(Of BlockSpan)
            Dim spans = ArrayBuilder(Of BlockSpan).GetInstance()
            CollectCommentsRegions(triviaList, spans)
            Return spans.ToImmutableAndFree()
        End Function

        Friend Sub CollectCommentsRegions(triviaList As SyntaxTriviaList,
                                          spans As ArrayBuilder(Of BlockSpan))
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
                            spans.AddIfNotNull(CreateCommentsRegion(startComment.Value, endComment.Value))
                            startComment = Nothing
                            endComment = Nothing
                        End If
                    End If
                Next

                ' Add any final span
                If startComment IsNot Nothing Then
                    spans.AddIfNotNull(CreateCommentsRegion(startComment.Value, endComment.Value))
                End If
            End If
        End Sub

        Friend Sub CollectCommentsRegions(node As SyntaxNode,
                                          spans As ArrayBuilder(Of BlockSpan))
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim triviaList = node.GetLeadingTrivia()

            CollectCommentsRegions(triviaList, spans)
        End Sub

        Friend Function CreateBlockSpan(
                span As TextSpan,
                hintSpan As TextSpan,
                bannerText As String,
                autoCollapse As Boolean,
                type As String,
                isCollapsible As Boolean,
                isDefaultCollapsed As Boolean) As BlockSpan?
            Return New BlockSpan(
                textSpan:=span,
                hintSpan:=hintSpan,
                bannerText:=bannerText,
                autoCollapse:=autoCollapse,
                isDefaultCollapsed:=isDefaultCollapsed,
                type:=type,
                isCollapsible:=isCollapsible)
        End Function

        Friend Function CreateBlockSpanFromBlock(
                blockNode As SyntaxNode,
                bannerText As String,
                autoCollapse As Boolean,
                type As String,
                isCollapsible As Boolean) As BlockSpan?
            Return CreateBlockSpan(
                blockNode.Span, GetHintSpan(blockNode),
                bannerText, autoCollapse,
                type, isCollapsible, isDefaultCollapsed:=False)
        End Function

        Friend Function CreateBlockSpanFromBlock(
                blockNode As SyntaxNode,
                bannerNode As SyntaxNode,
                autoCollapse As Boolean,
                type As String,
                isCollapsible As Boolean) As BlockSpan?
            Return CreateBlockSpan(
                blockNode.Span, GetHintSpan(blockNode),
                GetNodeBannerText(bannerNode),
                autoCollapse, type, isCollapsible, isDefaultCollapsed:=False)
        End Function

        Private Function GetHintSpan(blockNode As SyntaxNode) As TextSpan
            ' Don't include attributes in the hint-span for a block.  We don't want
            ' the attributes to show up when users hover over indent guide lines.
            Dim firstToken = blockNode.GetFirstToken()
            If firstToken.Kind() = SyntaxKind.LessThanToken AndAlso
               firstToken.Parent.IsKind(SyntaxKind.AttributeList) Then

                Dim attributeOwner = firstToken.Parent.Parent
                For Each child In attributeOwner.ChildNodesAndTokens
                    If child.Kind() <> SyntaxKind.AttributeList Then
                        Return TextSpan.FromBounds(child.SpanStart, blockNode.Span.End)
                    End If
                Next
            End If

            Return blockNode.Span
        End Function
    End Module
End Namespace
