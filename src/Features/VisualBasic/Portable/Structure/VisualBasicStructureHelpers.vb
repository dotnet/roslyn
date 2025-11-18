' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
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
                                          spans As ArrayBuilder(Of BlockSpan),
                                          options As BlockStructureOptions)
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim span As BlockSpan = Nothing
            If options.IsMetadataAsSource AndAlso TryGetLeadingCollapsibleSpan(node, span) Then
                spans.Add(span)
            Else
                Dim triviaList = node.GetLeadingTrivia()
                CollectCommentsRegions(triviaList, spans)
            End If
        End Sub

        Private Function TryGetLeadingCollapsibleSpan(node As SyntaxNode, <[Out]> ByRef span As BlockSpan) As Boolean
            Dim startToken = node.GetFirstToken()
            Dim endToken = GetEndToken(node)
            If startToken.IsKind(SyntaxKind.None) OrElse endToken.IsKind(SyntaxKind.None) Then
                ' if valid tokens can't be found then a meaningful span can't be generated
                span = Nothing
                Return False
            End If

            Dim firstComment = startToken.LeadingTrivia.FirstOrNull(Function(t) t.Kind = SyntaxKind.CommentTrivia)

            Dim startPosition = If(firstComment.HasValue,
                                   firstComment.Value.SpanStart,
                                   startToken.SpanStart)

            Dim endPosition = endToken.SpanStart

            ' TODO (tomescht): Mark the regions to be collapsed by default.
            If startPosition <> endPosition Then
                Dim hintTextEndToken = GetHintTextEndToken(node)
                span = New BlockSpan(
                    isCollapsible:=True,
                    type:=BlockTypes.Comment,
                    textSpan:=TextSpan.FromBounds(startPosition, endPosition),
                    hintSpan:=TextSpan.FromBounds(startPosition, hintTextEndToken.Span.End),
                    bannerText:=Ellipsis,
                    autoCollapse:=True)
                Return True
            End If

            span = Nothing
            Return False
        End Function

        Private Function GetEndToken(node As SyntaxNode) As SyntaxToken
            If node.IsKind(SyntaxKind.SubNewStatement) Then
                Dim subNewStatement = DirectCast(node, SubNewStatementSyntax)
                Return If(subNewStatement.Modifiers.FirstOrNull(), subNewStatement.DeclarationKeyword)
            ElseIf node.IsKind(SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement) Then
                Dim delegateStatement = DirectCast(node, DelegateStatementSyntax)
                Return If(delegateStatement.Modifiers.FirstOrNull(), delegateStatement.DelegateKeyword)
            ElseIf node.IsKind(SyntaxKind.EnumStatement) Then
                Dim enumStatement = DirectCast(node, EnumStatementSyntax)
                Return If(enumStatement.Modifiers.FirstOrNull(), enumStatement.EnumKeyword)
            ElseIf node.IsKind(SyntaxKind.EnumMemberDeclaration) Then
                Dim enumMemberDeclaration = DirectCast(node, EnumMemberDeclarationSyntax)
                Return enumMemberDeclaration.Identifier
            ElseIf node.IsKind(SyntaxKind.EventStatement) Then
                Dim eventStatement = DirectCast(node, EventStatementSyntax)
                Return If(eventStatement.Modifiers.FirstOrNull(),
                    If(eventStatement.CustomKeyword.IsKind(SyntaxKind.None), eventStatement.DeclarationKeyword, eventStatement.CustomKeyword))
            ElseIf node.IsKind(SyntaxKind.FieldDeclaration) Then
                Dim fieldDeclaration = DirectCast(node, FieldDeclarationSyntax)
                Return If(fieldDeclaration.Modifiers.FirstOrNull(), fieldDeclaration.Declarators.First().GetFirstToken())
            ElseIf node.IsKind(SyntaxKind.SubStatement, SyntaxKind.FunctionStatement) Then
                Dim methodStatement = DirectCast(node, MethodStatementSyntax)
                Return If(methodStatement.Modifiers.FirstOrNull(), methodStatement.DeclarationKeyword)
            ElseIf node.IsKind(SyntaxKind.OperatorStatement) Then
                Dim operatorStatement = DirectCast(node, OperatorStatementSyntax)
                Return If(operatorStatement.Modifiers.FirstOrNull(), operatorStatement.DeclarationKeyword)
            ElseIf node.IsKind(SyntaxKind.PropertyStatement) Then
                Dim propertyStatement = DirectCast(node, PropertyStatementSyntax)
                Return If(propertyStatement.Modifiers.FirstOrNull(), propertyStatement.DeclarationKeyword)
            ElseIf node.IsKind(SyntaxKind.ClassStatement, SyntaxKind.StructureStatement, SyntaxKind.InterfaceStatement, SyntaxKind.ModuleStatement) Then
                Dim typeStatement = DirectCast(node, TypeStatementSyntax)
                Return If(typeStatement.Modifiers.FirstOrNull(), typeStatement.DeclarationKeyword)
            Else
                Return Nothing
            End If
        End Function

        Private Function GetHintTextEndToken(node As SyntaxNode) As SyntaxToken
            Return node.GetLastToken()
        End Function

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
