' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class VisualBasicSyntaxTreeFactoryServiceFactory

        Partial Friend Class VisualBasicSyntaxTreeFactoryService

            ''' <summary>
            ''' Represents a syntax reference that doesn't actually hold onto the 
            ''' referenced node.  Instead, enough data is held onto so that the node
            ''' can be recovered and returned if necessary.
            ''' </summary>
            Private Class PathSyntaxReference
                Inherits SyntaxReference

                Private ReadOnly _tree As SyntaxTree

                Private ReadOnly _kind As SyntaxKind

                Private ReadOnly _span As TextSpan

                Private ReadOnly _pathFromRoot As ImmutableArray(Of Integer)

                Public Sub New(tree As SyntaxTree, node As SyntaxNode)
                    Me._tree = tree
                    Me._kind = node.Kind()
                    Me._span = node.Span
                    Me._pathFromRoot = ComputePathFromRoot(node)
                End Sub

                Public Overrides ReadOnly Property SyntaxTree As SyntaxTree
                    Get
                        Return Me._tree
                    End Get
                End Property

                Public Overrides ReadOnly Property Span As TextSpan
                    Get
                        Return Me._span
                    End Get
                End Property

                Private Function ComputePathFromRoot(node As SyntaxNode) As ImmutableArray(Of Integer)
                    Dim path = New List(Of Integer)()
                    Dim root = _tree.GetRoot()
                    While node IsNot root
                        While node.Parent IsNot Nothing
                            Dim index = GetChildIndex(node)
                            path.Add(index)
                            node = node.Parent
                        End While

                        If node.IsStructuredTrivia Then
                            Dim trivia = (DirectCast(node, StructuredTriviaSyntax)).ParentTrivia
                            Dim triviaIndex = GetTriviaIndex(trivia)
                            path.Add(triviaIndex)
                            Dim tokenIndex = GetChildIndex(trivia.Token)
                            path.Add(tokenIndex)
                            node = trivia.Token.Parent
                            Continue While
                        ElseIf node IsNot root Then
                            Throw New InvalidOperationException(VBWorkspaceResources.NodeDoesNotDescendFromRoot)
                        End If
                    End While

                    path.Reverse()
                    Return path.ToImmutableArray()
                End Function

                Private Function GetChildIndex(child As SyntaxNodeOrToken) As Integer
                    Dim parent As SyntaxNode = child.Parent
                    Dim index As Integer = 0
                    For Each nodeOrToken In parent.ChildNodesAndTokens()
                        If nodeOrToken = child Then
                            Return index
                        End If

                        index = index + 1
                    Next

                    Throw New InvalidOperationException(VBWorkspaceResources.NodeNotInParentsChildList)
                End Function

                Private Function GetTriviaIndex(trivia As SyntaxTrivia) As Integer
                    Dim token = trivia.Token
                    Dim index As Integer = 0
                    For Each tr In token.LeadingTrivia
                        If tr = trivia Then
                            Return index
                        End If

                        index = index + 1
                    Next

                    For Each tr In token.TrailingTrivia
                        If tr = trivia Then
                            Return index
                        End If

                        index = index + 1
                    Next

                    Throw New InvalidOperationException(VBWorkspaceResources.TriviaIsNotAssociatedWithToken)
                End Function

                Private Function GetTrivia(token As SyntaxToken, triviaIndex As Integer) As SyntaxTrivia
                    Dim leadingCount = token.LeadingTrivia.Count
                    If triviaIndex <= leadingCount Then
                        Return token.LeadingTrivia.ElementAt(triviaIndex)
                    End If

                    triviaIndex -= leadingCount
                    Return token.TrailingTrivia.ElementAt(triviaIndex)
                End Function

                Public Overrides Function GetSyntax(Optional cancellationToken As CancellationToken = Nothing) As SyntaxNode
                    Return DirectCast(Me.GetNode(Me._tree.GetRoot(cancellationToken)), SyntaxNode)
                End Function

                Public Overrides Async Function GetSyntaxAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of SyntaxNode)
                    Dim root = Await Me._tree.GetRootAsync(cancellationToken).ConfigureAwait(False)
                    Return Me.GetNode(root)
                End Function

                Private Function GetNode(root As SyntaxNode) As SyntaxNode
                    Dim node = root
                    Dim i As Integer = 0
                    Dim n As Integer = Me._pathFromRoot.Length

                    While i < n
                        Dim child = node.ChildNodesAndTokens()(Me._pathFromRoot(i))

                        If child.IsToken Then
                            i = i + 1
                            System.Diagnostics.Debug.Assert(i < n)
                            Dim triviaIndex = Me._pathFromRoot(i)
                            Dim trivia = GetTrivia(child.AsToken(), triviaIndex)
                            node = trivia.GetStructure()
                        Else
                            node = child.AsNode()
                        End If

                        i = i + 1
                    End While

                    System.Diagnostics.Debug.Assert(node.Kind = Me._kind)
                    System.Diagnostics.Debug.Assert(node.Span = Me._span)
                    Return node

                End Function
            End Class
        End Class
    End Class
End Namespace
