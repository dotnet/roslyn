' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Friend Partial Class SyntaxList
        Friend Class SeparatedWithManyChildren
            Inherits SyntaxList

            Private ReadOnly _children As ArrayElement(Of SyntaxNode)()

            Friend Sub New(green As InternalSyntax.SyntaxList, parent As SyntaxNode, position As Integer)
                MyBase.New(green, parent, position)
                Me._children = New ArrayElement(Of SyntaxNode)(((green.SlotCount + 1) >> 1) - 1) {}
            End Sub

            Friend Overrides Function GetNodeSlot(i As Integer) As SyntaxNode
                If (i And 1) <> 0 Then
                    'separator
                    Return Nothing
                End If

                Return GetRedElement(Me._children(i >> 1).Value, i)
            End Function

            Friend Overrides Function GetCachedSlot(i As Integer) As SyntaxNode
                If (i And 1) <> 0 Then
                    'separator
                    Return Nothing
                End If

                Return TryCast(Me._children(i >> 1).Value, VisualBasicSyntaxNode)
            End Function

            Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSyntaxVisitor(Of TResult)) As TResult
                Throw New NotImplementedException()
            End Function

            Public Overrides Sub Accept(visitor As VisualBasicSyntaxVisitor)
                Throw New NotImplementedException()
            End Sub
        End Class
    End Class
End Namespace
