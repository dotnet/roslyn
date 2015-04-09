' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------------------------------------
'  Contains syntax tree traversal methods.
'-----------------------------------------------------------------------------------------------------------

Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public Class VisualBasicSyntaxNode

        Friend Function FindTokenInternal(position As Integer) As SyntaxToken
            ' While maintaining invariant   curNode.FullSpan.Start <= position < curNode.FullSpan.End
            ' go down the tree until a token is found
            Dim curNode As SyntaxNodeOrToken = Me
            Do
                Debug.Assert(Not curNode.IsKind(SyntaxKind.None))
                Debug.Assert(curNode.FullSpan.Contains(position))

                Dim node = curNode.AsNode

                If node IsNot Nothing Then
                    'find a child that includes the position
                    curNode = node.ChildThatContainsPosition(position)
                    Debug.Assert(Not curNode.IsKind(SyntaxKind.None), "Node overlaps position, but children are not? The tree must be corrupted.")
                Else
                    Return curNode.AsToken
                End If
            Loop
        End Function

        ''' <summary>
        ''' Finds a token according to the following rules:
        ''' 1)	If position matches the End of the node's Span, then its last token is returned. 
        ''' 
        ''' 2)	If node.FullSpan.Contains(position) the token that contains given position is returned.
        '''     If stepInto is not Nothing, then structured trivia that satisfies the condition will also be visited during the search.
        ''' 
        ''' 3)	Otherwise an IndexOutOfRange is thrown
        ''' </summary>
        Private Shadows Function FindToken(position As Integer, stepInto As Func(Of SyntaxTrivia, Boolean)) As SyntaxToken
            Dim tk = FindToken(position, False)
            If stepInto Is Nothing Then
                Return tk
            End If

            Dim trivia As SyntaxTrivia = Nothing
            Dim span = tk.Span
            If position < span.Start AndAlso tk.HasLeadingTrivia Then
                ' token may be in leading trivia
                trivia = GetTriviaThatContainsPosition(tk.LeadingTrivia, position)
            ElseIf position >= span.End AndAlso tk.HasTrailingTrivia Then
                ' token may be in trailing trivia
                trivia = GetTriviaThatContainsPosition(tk.TrailingTrivia, position)
            End If

            If trivia.HasStructure AndAlso stepInto(trivia) Then
                tk = DirectCast(trivia.GetStructure(), VisualBasicSyntaxNode).FindTokenInternal(position)
            End If

            Return tk
        End Function

        Private Function TryGetLastTokenAt(position As Integer, ByRef lastToken As SyntaxToken) As Boolean
            If position = Me.EndPosition Then
                Dim cu = TryCast(Me, CompilationUnitSyntax)
                If cu IsNot Nothing Then
                    lastToken = cu.EndOfFileToken
                    Debug.Assert(lastToken.EndPosition = position)
                Else
                    lastToken = Me.GetLastToken
                    If (lastToken.EndPosition <> position) Then
                        Return False
                    End If
                End If

                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Finds a token according to the following rules:
        ''' 1)	If position matches the End of the node's Span, then its last token is returned. 
        ''' 
        ''' 2)	If node.FullSpan.Contains(position) then the token that contains given position is returned.
        ''' 
        ''' 3)	Otherwise an IndexOutOfRange is thrown
        ''' </summary>
        Public Shadows Function FindToken(position As Integer, Optional findInsideTrivia As Boolean = False) As SyntaxToken
            If findInsideTrivia Then
                Return FindToken(position, SyntaxTrivia.Any)
            Else
                Dim lastToken As SyntaxToken = Nothing
                If TryGetLastTokenAt(position, lastToken) Then
                    Return lastToken
                End If

                If Not Me.FullSpan.Contains(position) Then
                    Throw New IndexOutOfRangeException(NameOf(position))
                End If

                Return FindTokenInternal(position)
            End If
        End Function

        Public Shadows Function FindTrivia(textPosition As Integer, Optional findInsideTrivia As Boolean = False) As SyntaxTrivia
            Dim tk = FindToken(textPosition, findInsideTrivia)
            If tk.Kind = SyntaxKind.None Then
                Return Nothing  ' no tokens or trivia at this position
            End If

            Dim span = tk.Span
            If span.Contains(textPosition) Then
                Return Nothing  ' position is used by the token itself
            End If

            Dim trivia = If(textPosition < span.Start,
                            GetTriviaThatContainsPosition(tk.LeadingTrivia, textPosition),
                            GetTriviaThatContainsPosition(tk.TrailingTrivia, textPosition))

            Return trivia
        End Function

        Friend Shared Function GetIndexOfChildThatContainsPosition(
                                        list As ChildSyntaxList,
                                        position As Integer) As Integer

            Dim l As Integer = 0
            Dim r As Integer = list.Count - 1

            Do While (l <= r)
                Dim m As Integer = (l + ((r - l) >> 1))
                Dim node = list.Item(m)

                If position < node.Position Then
                    r = (m - 1)
                    Continue Do
                End If
                If position >= node.EndPosition Then
                    l = (m + 1)
                    Continue Do
                End If
                Return m
            Loop

            Return -1
        End Function

        Friend Shared Function GetTriviaThatContainsPosition(
                                    list As SyntaxTriviaList,
                                    position As Integer) As SyntaxTrivia

            For Each trivia In list
                If trivia.FullSpan.Contains(position) Then
                    Return trivia
                End If

                If trivia.Position > position Then
                    Exit For
                End If
            Next

            Return Nothing
        End Function


        Private Shared Sub PushNode(stack As Stack(Of SyntaxNodeOrToken),
                                      node As SyntaxNodeOrToken)

            stack.Push(node)
        End Sub

        Private Shared Sub PushNode(stack As Stack(Of SyntaxNodeOrToken),
                                      node As SyntaxNodeOrToken,
                                      span As TextSpan)

            If span.IntersectsWith(node.FullSpan) Then
                stack.Push(node)
            End If
        End Sub

        Private Shared Sub PushNodes(stack As Stack(Of SyntaxNodeOrToken),
                                   nodes As ChildSyntaxList)
            If nodes.Count > 0 Then
                For i = nodes.Count - 1 To 0 Step -1
                    PushNode(stack, ChildSyntaxList.ItemInternal(nodes.Node, i))
                Next
            End If
        End Sub

        Private Shared Sub PushNodes(stack As Stack(Of SyntaxNodeOrToken),
                                           nodes As ChildSyntaxList,
                                           span As TextSpan)
            If nodes.Count > 0 Then
                Dim left = 0
                Dim right = nodes.Count - 1
                'we do not want to put all children on stack if there are too many
                'TODO: how many is many?
                If nodes.Count > 16 Then
                    ' all nodes that might "intersect" with the span
                    Dim inclusiveStart = Math.Max(span.Start - 1, 0)
                    Dim inclusiveEnd = span.End

                    left = GetIndexOfChildThatContainsPosition(nodes, inclusiveStart)
                    right = GetIndexOfChildThatContainsPosition(nodes, inclusiveEnd)
                    If (left = -1) Then
                        ' all children start after the span starts.
                        left = 0
                    End If

                    If (right = -1) Then
                        ' All children end before the span ends. 
                        right = nodes.Count - 1
                    End If
                End If

                For i = right To left Step -1
                    PushNode(stack, ChildSyntaxList.ItemInternal(nodes.Node, i), span)
                Next
            End If
        End Sub

    End Class

End Namespace
