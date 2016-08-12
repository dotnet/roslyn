' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Friend MustInherit Class SyntaxList
        Inherits VisualBasicSyntaxNode

        Friend Sub New(green As InternalSyntax.VisualBasicSyntaxNode, parent As SyntaxNode, position As Integer)
            MyBase.New(green, parent, position)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSyntaxVisitor(Of TResult)) As TResult
            Throw New NotImplementedException()
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSyntaxVisitor)
            Throw New NotImplementedException()
        End Sub
    End Class

    Friend Class SyntaxListBuilder
        Inherits AbstractSyntaxListBuilder

        Friend Sub New(size As Integer)
            MyBase.New(size)
        End Sub

        Friend Overrides Function ToListNode() As GreenNode
            Select Case Me.Count
                Case 0
                    Return Nothing
                Case 1
                    Return DirectCast(Me.Nodes(0).Value, InternalSyntax.VisualBasicSyntaxNode)
                Case 2
                    Return InternalSyntax.SyntaxList.List(
                        DirectCast(Me.Nodes(0).Value, InternalSyntax.VisualBasicSyntaxNode),
                        DirectCast(Me.Nodes(1).Value, InternalSyntax.VisualBasicSyntaxNode))
                Case 3
                    Return InternalSyntax.SyntaxList.List(
                        DirectCast(Me.Nodes(0).Value, InternalSyntax.VisualBasicSyntaxNode),
                        DirectCast(Me.Nodes(1).Value, InternalSyntax.VisualBasicSyntaxNode),
                        DirectCast(Me.Nodes(2).Value, InternalSyntax.VisualBasicSyntaxNode))
            End Select

            Dim tmp = New ArrayElement(Of Syntax.InternalSyntax.VisualBasicSyntaxNode)(Me.Count) {}
            For i = 0 To Me.Count - 1
                tmp(i).Value = DirectCast(Nodes(i).Value, Syntax.InternalSyntax.VisualBasicSyntaxNode)
            Next

            Return Syntax.InternalSyntax.SyntaxList.List(tmp)
        End Function
    End Class
End Namespace