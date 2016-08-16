' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Friend Class SyntaxListBuilder
        Inherits AbstractSyntaxListBuilder

        Friend Sub New(size As Integer)
            MyBase.New(size)
        End Sub

        Friend Shadows Function Any(kind As SyntaxKind) As Boolean
            Return MyBase.Any(kind)
        End Function

        Friend Sub RemoveLast()
            Me.Count -= 1
            Me.Nodes(Count) = Nothing
        End Sub

        Friend Function ToGreenArray() As ArrayElement(Of GreenNode)()
            Dim array = New ArrayElement(Of GreenNode)(Me.Count - 1) {}
            Dim i As Integer
            For i = 0 To array.Length - 1
                array(i).Value = DirectCast(Me.Nodes(i).Value, InternalSyntax.VisualBasicSyntaxNode)
            Next i
            Return array
        End Function

        Friend Function ToListNode() As GreenNode
            Select Case Me.Count
                Case 0
                    Return Nothing
                Case 1
                    Return DirectCast(Me.Nodes(0).Value, InternalSyntax.VisualBasicSyntaxNode)
                Case 2
                    Return Microsoft.CodeAnalysis.Syntax.InternalSyntax.CommonSyntaxList.List(DirectCast(Me.Nodes(0).Value, InternalSyntax.VisualBasicSyntaxNode), DirectCast(Me.Nodes(1).Value, InternalSyntax.VisualBasicSyntaxNode))
                Case 3
                    Return Microsoft.CodeAnalysis.Syntax.InternalSyntax.CommonSyntaxList.List(DirectCast(Me.Nodes(0).Value, InternalSyntax.VisualBasicSyntaxNode), DirectCast(Me.Nodes(1).Value, InternalSyntax.VisualBasicSyntaxNode), DirectCast(Me.Nodes(2).Value, InternalSyntax.VisualBasicSyntaxNode))
            End Select

            Return Microsoft.CodeAnalysis.Syntax.InternalSyntax.CommonSyntaxList.List(Me.ToGreenArray)
        End Function
    End Class
End Namespace