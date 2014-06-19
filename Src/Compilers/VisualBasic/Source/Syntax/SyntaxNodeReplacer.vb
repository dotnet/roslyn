'-----------------------------------------------------------------------------------------------------------
'
'  Copyright (c) Microsoft Corporation.  All rights reserved.
'
'-----------------------------------------------------------------------------------------------------------
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Friend Class SyntaxNodeReplacer
        Friend Shared Function Replace(Of TRoot As SyntaxNode, TNode As SyntaxNode)(root As TRoot, oldNode As TNode, newNode As TNode) As TRoot
            If oldNode Is newNode Then
                Return root
            End If

            Return DirectCast(New SingleNodeReplacer(oldNode, newNode).Visit(root), TRoot)
        End Function

        Friend Shared Function Replace(Of TRoot As SyntaxNode, TNode As SyntaxNode)(root As TRoot, oldNodes As IEnumerable(Of TNode), computeReplacementNode As Func(Of TNode, TNode, SyntaxNode)) As TRoot
            Dim oldNodesArray = oldNodes.ToArray()
            If oldNodesArray.Length = 0 Then
                Return root
            End If

            Return DirectCast(New MultipleNodeReplacer(oldNodesArray, Function(node, rewritten) computeReplacementNode(DirectCast(node, TNode), DirectCast(rewritten, TNode))).Visit(root), TRoot)
        End Function

        Private Class ReplacerBase
            Inherits SyntaxRewriter

            Public Sub New(visitIntoStructuredTrivia As Boolean)
                MyBase.New(visitIntoStructuredTrivia)
            End Sub

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                If Me.VisitIntoStructuredTrivia Then
                    Return MyBase.VisitToken(token)
                Else
                    Return token
                End If
            End Function
        End Class

        Private Class SingleNodeReplacer
            Inherits ReplacerBase

            Private ReadOnly oldNode As SyntaxNode

            Private ReadOnly newNode As SyntaxNode

            Private ReadOnly oldNodeFullSpan As TextSpan

            Public Sub New(oldNode As SyntaxNode, newNode As SyntaxNode)
                MyBase.New(oldNode.IsPartOfStructuredTrivia())
                Me.oldNode = oldNode
                Me.newNode = newNode
                Me.oldNodeFullSpan = oldNode.FullSpan
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                If node IsNot Nothing Then
                    If node Is Me.oldNode Then
                        Return Me.newNode
                    End If

                    If node.FullSpan.IntersectsWith(Me.oldNodeFullSpan) Then
                        Return MyBase.Visit(node)
                    End If
                End If

                Return node
            End Function
        End Class

        Private Class MultipleNodeReplacer
            Inherits ReplacerBase

            Private ReadOnly nodes As SyntaxNode()

            Private ReadOnly nodeSet As HashSet(Of SyntaxNode)

            Private ReadOnly totalSpan As TextSpan

            Private ReadOnly computeReplacementNode As Func(Of SyntaxNode, SyntaxNode, SyntaxNode)

            Public Sub New(nodes As SyntaxNode(), computeReplacementNode As Func(Of SyntaxNode, SyntaxNode, SyntaxNode))
                MyBase.New(nodes.Any(Function(n) n.IsPartOfStructuredTrivia()))
                Me.nodes = nodes
                Me.nodeSet = New HashSet(Of SyntaxNode)(Me.nodes)
                Me.totalSpan = ComputeTotalSpan(Me.nodes)
                Me.computeReplacementNode = computeReplacementNode
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                Dim result = node
                If node IsNot Nothing Then
                    If Me.ShouldVisit(node) Then
                        result = MyBase.Visit(node)
                    End If

                    If Me.nodeSet.Contains(node) Then
                        result = Me.computeReplacementNode(node, result)
                    End If
                End If

                Return result
            End Function

            Private Shared Function ComputeTotalSpan(nodes As SyntaxNode()) As TextSpan
                Dim span0 = nodes(0).FullSpan
                Dim start As Integer = span0.Start
                Dim [end] As Integer = span0.End
                Dim i As Integer = 1

                While i < nodes.Length
                    Dim span = nodes(i).FullSpan
                    start = Math.Min(start, span.Start)
                    [end] = Math.Max([end], span.End)
                    i = i + 1
                End While

                Return New TextSpan(start, [end] - start)
            End Function

            Private Function ShouldVisit(node As SyntaxNode) As Boolean
                Dim span = node.Span
                If Not span.IntersectsWith(Me.totalSpan) Then
                    Return False
                End If

                For Each n In Me.nodes
                    If span.IntersectsWith(n.FullSpan) Then
                        Return True
                    End If
                Next

                Return False
            End Function
        End Class
    End Class
End Namespace
