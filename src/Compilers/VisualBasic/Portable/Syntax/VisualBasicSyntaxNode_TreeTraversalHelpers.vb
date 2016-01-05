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
        ''' <summary>
        ''' Finds a token according to the following rules:
        ''' 1)	If position matches the End of the node's Span, then its last token is returned. 
        ''' 
        ''' 2)	If node.FullSpan.Contains(position) then the token that contains given position is returned.
        ''' 
        ''' 3)	Otherwise an IndexOutOfRange is thrown
        ''' </summary>
        Public Shadows Function FindToken(position As Integer, Optional findInsideTrivia As Boolean = False) As SyntaxToken
            Return MyBase.FindToken(position, findInsideTrivia)
        End Function

        Public Shadows Function FindTrivia(textPosition As Integer, Optional findInsideTrivia As Boolean = False) As SyntaxTrivia
            Return MyBase.FindTrivia(textPosition, findInsideTrivia)
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
