' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    Partial Class StatementSyntaxComparer
        ''' <summary>
        ''' Compares a body of nodes that have multiple bodies.
        ''' The comparer points to the body to compare.
        ''' </summary>
        Friend NotInheritable Class MultiBody
            Inherits StatementSyntaxComparer

            Private ReadOnly oldBody As SyntaxNode
            Private ReadOnly newBody As SyntaxNode
            Private ReadOnly oldRoot As SyntaxNode
            Private ReadOnly newRoot As SyntaxNode

            Friend Sub New(oldBody As SyntaxNode, newBody As SyntaxNode)
                Me.oldBody = oldBody
                Me.newBody = newBody
                Me.oldRoot = oldBody.Parent
                Me.newRoot = newBody.Parent
            End Sub

            Protected Overrides Function GetChildren(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
                Debug.Assert(GetLabel(node) <> IgnoredNode)

                If node Is oldRoot OrElse node Is newRoot Then
                    Return EnumerateRootChildren(If(node Is oldRoot, oldBody, newBody))
                End If

                Return If(NonRootHasChildren(node), EnumerateChildren(node), Nothing)
            End Function

            Private Shared Iterator Function EnumerateRootChildren(childNode As SyntaxNode) As IEnumerable(Of SyntaxNode)
                If GetLabelImpl(childNode) <> Label.Ignored Then
                    Yield childNode
                Else
                    For Each descendant In childNode.DescendantNodesAndTokens(AddressOf SyntaxUtilities.IsNotLambda)
                        If SyntaxUtilities.IsLambda(descendant.Kind) Then
                            Yield descendant.AsNode()
                        End If
                    Next
                End If
            End Function

            Protected Overrides Iterator Function GetDescendants(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
                If node Is oldRoot OrElse node Is newRoot Then
                    Dim descendantNode = If(node Is oldRoot, oldBody, newBody)
                    If GetLabelImpl(descendantNode) <> Label.Ignored Then
                        Yield descendantNode
                    End If

                    node = descendantNode
                End If

                For Each descendant In node.DescendantNodesAndTokens(
                    descendIntoChildren:=AddressOf NonRootHasChildren,
                    descendIntoTrivia:=False)

                    Dim descendantNode = descendant.AsNode()
                    If descendantNode IsNot Nothing AndAlso GetLabel(descendantNode) <> IgnoredNode Then
                        Yield descendantNode
                    End If
                Next
            End Function
        End Class
    End Class
End Namespace
