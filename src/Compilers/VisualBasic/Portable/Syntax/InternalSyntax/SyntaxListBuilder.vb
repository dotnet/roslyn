'' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax

'Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
'    Friend Class SyntaxListBuilder
'        Inherits AbstractSyntaxListBuilder(Of VisualBasicSyntaxNode, SyntaxList(Of VisualBasicSyntaxNode))

'        Public Shared Function Create() As SyntaxListBuilder
'            Return New SyntaxListBuilder(8)
'        End Function

'        Public Sub New(size As Integer)
'            MyBase.New(size)
'        End Sub

'        Public Shadows Sub AddRange(Of TNode As VisualBasicSyntaxNode)(list As SyntaxList(Of TNode))
'            Me.AddRange(list, 0, list.Count)
'        End Sub

'        Public Shadows Sub AddRange(Of TNode As VisualBasicSyntaxNode)(
'            list As SyntaxList(Of TNode),
'            offset As Integer,
'            length As Integer)

'            MyBase.AddRange(New SyntaxList(Of VisualBasicSyntaxNode)(list.Node), offset, length)
'        End Sub

'        Public Shadows Function Any(kind As SyntaxKind) As Boolean
'            Return MyBase.Any(kind)
'        End Function

'        Friend Function ToListNode() As VisualBasicSyntaxNode
'            Select Case Me.Count
'                Case 0
'                    Return Nothing
'                Case 1
'                    Return Me.Nodes(0)
'                Case 2
'                    Return SyntaxList.List(Me.Nodes(0), Me.Nodes(1))
'                Case 3
'                    Return SyntaxList.List(Me.Nodes(0), Me.Nodes(1), Me.Nodes(2))
'            End Select
'            Return SyntaxList.List(Me.ToArray)
'        End Function

'        Public Function ToList() As SyntaxList(Of VisualBasicSyntaxNode)
'            Return New SyntaxList(Of VisualBasicSyntaxNode)(ToListNode)
'        End Function

'        Public Function ToList(Of TDerived As VisualBasicSyntaxNode)() As SyntaxList(Of TDerived)
'            Return New SyntaxList(Of TDerived)(ToListNode)
'        End Function
'    End Class
'End Namespace