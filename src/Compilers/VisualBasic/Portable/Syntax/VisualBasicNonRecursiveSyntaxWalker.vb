' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Represents a <see cref="VisualBasicSyntaxVisitor"/> that descends an entire <see cref="SyntaxNode"/> tree
    ''' visiting each SyntaxNode and its child <see cref="SyntaxNode"/>s and <see cref="SyntaxToken"/>s in depth-first order.
    ''' </summary>
    Public MustInherit Class VisualBasicNonRecursiveSyntaxWalker
        Inherits VisualBasicSyntaxVisitor

        Private ReadOnly _stack As Stack(Of SyntaxNodeOrToken) = New Stack(Of SyntaxNodeOrToken)()

        Public Overrides Sub Visit(node As SyntaxNode)
            Dim stackStart = _stack.Count
            _stack.Push(node)
            While _stack.Count > stackStart
                Dim n = _stack.Pop()
                If n.IsToken Then
                    Me.VisitToken(n.AsToken())
                ElseIf (Me.ShouldVisitChildren(n.AsNode())) Then
                    Me.VisitNode(n.AsNode())
                    Dim children = n.ChildNodesAndTokens()
                    For i = children.Count - 1 To 0 Step -1
                        _stack.Push(children(i))
                    Next
                End If
            End While
        End Sub

        Protected Overridable Function ShouldVisitChildren(node As SyntaxNode) As Boolean
            Return True
        End Function

        Public Overridable Sub VisitNode(node As SyntaxNode)
            DirectCast(node, VisualBasicSyntaxNode).Accept(Me)
        End Sub

        Public Overridable Sub VisitToken(node As SyntaxToken)
        End Sub
    End Class
End Namespace