' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class VisualBasicSyntaxTreeFactoryServiceFactory

        Partial Friend Class VisualBasicSyntaxTreeFactoryService

            ''' <summary>
            ''' Represents a syntax reference that doesn't actually hold onto the referenced node.
            ''' Instead, enough data is held onto so that the node can be recovered and returned if
            ''' necessary.
            ''' </summary>
            Private Class PositionalSyntaxReference
                Inherits SyntaxReference

                Private ReadOnly _tree As SyntaxTree
                Private ReadOnly _span As TextSpan
                Private ReadOnly _kind As SyntaxKind

                Public Sub New(tree As SyntaxTree, node As SyntaxNode)
                    _tree = tree
                    _span = node.Span
                    _kind = node.Kind()
                End Sub

                Public Overrides ReadOnly Property SyntaxTree As SyntaxTree
                    Get
                        Return _tree
                    End Get
                End Property

                Public Overrides ReadOnly Property Span As TextSpan
                    Get
                        Return _span
                    End Get
                End Property

                Public Overrides Function GetSyntax(Optional cancellationToken As CancellationToken = Nothing) As SyntaxNode
                    Return DirectCast(Me.GetNode(_tree.GetRoot(cancellationToken)), VisualBasicSyntaxNode)
                End Function

                Public Overrides Async Function GetSyntaxAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of SyntaxNode)
                    Dim root = Await _tree.GetRootAsync(cancellationToken).ConfigureAwait(False)
                    Return Me.GetNode(root)
                End Function

                Private Function GetNode(root As SyntaxNode) As SyntaxNode
                    ' Find our node going down in the tree. 
                    ' Try not going deeper than needed.
                    Dim current = root
                    Dim spanStart As Integer = Me._span.Start

                    While current.FullSpan.Contains(spanStart)
                        If current.Kind = Me._kind AndAlso current.Span = Me._span Then
                            Return current
                        End If

                        Dim nodeOrToken = current.ChildThatContainsPosition(spanStart)

                        ' we have got a token. It means that the node is in structured trivia
                        If nodeOrToken.IsToken Then
                            Return GetNodeInStructuredTrivia(current)
                        End If

                        current = nodeOrToken.AsNode
                    End While

                    Throw New InvalidOperationException("reference to a node that does not exist?")

                End Function

                Private Function GetNodeInStructuredTrivia(parent As SyntaxNode) As SyntaxNode
                    ' Syntax references to nonterminals in structured trivia should be uncommon.
                    ' Provide more efficient implementation if that is not true
                    Return parent.DescendantNodes(Me._span, descendIntoTrivia:=True).
                        First(Function(node)
                                  Return node.Kind = Me._kind AndAlso node.Span = Me._span
                              End Function)
                End Function
            End Class
        End Class
    End Class
End Namespace
