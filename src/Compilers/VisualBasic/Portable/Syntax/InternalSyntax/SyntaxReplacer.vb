' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Class FirstTokenReplacer
        Inherits VisualBasicSyntaxRewriter

        Private ReadOnly _newItem As Func(Of SyntaxToken, SyntaxToken)

        Private _isFirst As Boolean = True

        Private Sub New(newItem As Func(Of SyntaxToken, SyntaxToken))
            _newItem = newItem
        End Sub

        Friend Shared Function Replace(Of TTree As VisualBasicSyntaxNode)(
                    root As TTree,
                    newItem As Func(Of SyntaxToken, SyntaxToken)) As TTree

            Return DirectCast(New FirstTokenReplacer(newItem).Visit(root), TTree)
        End Function

        Public Overrides Function Visit(node As VisualBasicSyntaxNode) As VisualBasicSyntaxNode
            If node Is Nothing Then
                Return Nothing
            End If

            ' we are not interested in nodes that are not first
            If Not _isFirst Then
                Return node
            End If

            Dim result = MyBase.Visit(node)
            _isFirst = False

            Return result
        End Function

        Public Overrides Function VisitSyntaxToken(token As SyntaxToken) As SyntaxToken
            Return _newItem(token)
        End Function
    End Class

    Friend Class LastTokenReplacer
        Inherits VisualBasicSyntaxRewriter

        Private ReadOnly _newItem As Func(Of SyntaxToken, SyntaxToken)

        Private _skipCnt As Integer

        Private Sub New(newItem As Func(Of SyntaxToken, SyntaxToken))
            _newItem = newItem
        End Sub

        Friend Shared Function Replace(Of TTree As GreenNode)(
                    root As TTree,
                    newItem As Func(Of SyntaxToken, SyntaxToken)) As TTree

            Return DirectCast(New LastTokenReplacer(newItem).VisitGreen(root), TTree)
        End Function

        Public Overrides Function Visit(node As VisualBasicSyntaxNode) As VisualBasicSyntaxNode
            Return DirectCast(VisitGreen(node), VisualBasicSyntaxNode)
        End Function

        Private Function VisitGreen(node As GreenNode) As GreenNode
            If node Is Nothing Then
                Return Nothing
            End If

            ' node is not interesting until skip count is 0
            If _skipCnt <> 0 Then
                _skipCnt -= 1
                Return node
            End If            ' not interested in trivia

            If Not node.IsToken Then
                ' how many children should skip
                Dim allChildrenCnt = 0
                For i As Integer = 0 To node.SlotCount - 1
                    Dim child = node.GetSlot(i)

                    If child Is Nothing Then
                        Continue For
                    End If

                    If child.IsList Then
                        allChildrenCnt += child.SlotCount
                    Else
                        allChildrenCnt += 1
                    End If
                Next

                ' no children
                If allChildrenCnt = 0 Then
                    Return node
                End If

                Dim prevIdx = _skipCnt
                _skipCnt = allChildrenCnt - 1
                Dim result As GreenNode
                If node.IsList Then
                    result = VisitList(New CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(node)).Node
                Else
                    result = MyBase.Visit(DirectCast(node, VisualBasicSyntaxNode))
                End If

                _skipCnt = prevIdx
                Return result
            Else
                Return MyBase.Visit(DirectCast(node, SyntaxToken))
            End If

        End Function

        Public Overrides Function VisitSyntaxToken(token As SyntaxToken) As SyntaxToken
            Return _newItem(token)
        End Function
    End Class
End Namespace
