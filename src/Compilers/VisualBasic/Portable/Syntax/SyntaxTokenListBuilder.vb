' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Friend Class SyntaxTokenListBuilder
        Private _count As Integer
        Private _nodes As InternalSyntax.VisualBasicSyntaxNode()

        Public Sub New(size As Integer)
            _nodes = New InternalSyntax.VisualBasicSyntaxNode(size - 1) {}
            _count = 0
        End Sub

        Public Shared Function Create() As SyntaxTokenListBuilder
            Return New SyntaxTokenListBuilder(8)
        End Function

        Public ReadOnly Property Count As Integer
            Get
                Return _count
            End Get
        End Property

        Friend Function Add(item As InternalSyntax.SyntaxToken) As SyntaxTokenListBuilder
            Debug.Assert(item IsNot Nothing)
            If ((_nodes Is Nothing) OrElse (_count >= _nodes.Length)) Then
                Grow(If((_count = 0), 8, (_nodes.Length * 2)))
            End If
            _nodes(_count) = item
            _count += 1
            Return Me
        End Function

        Public Function Add(item As SyntaxToken) As SyntaxTokenListBuilder
            Return Add(DirectCast(item.Node, InternalSyntax.SyntaxToken))
        End Function

        Public Function Add(list As SyntaxTokenList) As SyntaxTokenListBuilder
            Return Add(list, 0, list.Count)
        End Function

        Public Function Add(list As SyntaxTokenList, offset As Integer, length As Integer) As SyntaxTokenListBuilder
            If ((_nodes Is Nothing) OrElse ((_count + length) > _nodes.Length)) Then
                Grow((_count + length))
            End If
            list.CopyTo(offset, _nodes, _count, length)
#If DEBUG Then
            For i = 0 To length - 1
                Debug.Assert(_nodes(_count + i) IsNot Nothing)
            Next
#End If
            _count = (_count + length)
            Return Me
        End Function

        Public Sub Add(list As SyntaxToken())
            Add(list, 0, list.Length)
        End Sub

        Public Sub Add(list As SyntaxToken(), offset As Integer, length As Integer)
            If ((_nodes Is Nothing) OrElse ((_count + length) > _nodes.Length)) Then
                Grow((_count + length))
            End If

            For i As Integer = 0 To length - 1
                _nodes(Count + i) = DirectCast(list(offset + i).Node, InternalSyntax.SyntaxToken)
            Next
            _count = (_count + length)
        End Sub

        Private Sub Grow(size As Integer)
            Dim tmp = New InternalSyntax.VisualBasicSyntaxNode(size - 1) {}
            Array.Copy(_nodes, tmp, _nodes.Length)
            _nodes = tmp
        End Sub

        Public Shared Widening Operator CType(builder As SyntaxTokenListBuilder) As SyntaxTokenList
            Return builder.ToList
        End Operator

        Public Function ToList() As SyntaxTokenList
            If (_count > 0) Then
                Select Case _count
                    Case 1
                        Return New SyntaxTokenList(Nothing, _nodes(0), 0, 0)
                    Case 2
                        Return New SyntaxTokenList(Nothing, InternalSyntax.SyntaxList.List(_nodes(0), _nodes(1)), 0, 0)
                    Case 3
                        Return New SyntaxTokenList(Nothing, InternalSyntax.SyntaxList.List(_nodes(0), _nodes(1), _nodes(2)), 0, 0)
                End Select
                Return New SyntaxTokenList(Nothing, InternalSyntax.SyntaxList.List(_nodes, _count), 0, 0)
            End If
            Return New SyntaxTokenList
        End Function

    End Class
End Namespace
