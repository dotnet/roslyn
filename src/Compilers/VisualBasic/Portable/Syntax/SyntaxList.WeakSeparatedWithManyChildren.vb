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
        Friend Class WeakSeparatedWithManyChildren
            Inherits SyntaxList

            Private ReadOnly _children As ArrayElement(Of WeakReference(Of SyntaxNode))()

            Friend Sub New(green As InternalSyntax.SyntaxList, parent As SyntaxNode, position As Integer)
                MyBase.New(green, parent, position)
                Me._children = New ArrayElement(Of WeakReference(Of SyntaxNode))(((green.SlotCount + 1) >> 1) - 1) {}
            End Sub

            Friend Overrides Function GetNodeSlot(i As Integer) As SyntaxNode
                Dim result As SyntaxNode = Nothing

                If (i And 1) = 0 Then
                    'not a separator
                    result = GetWeakRedElement(Me._children(i >> 1).Value, i)
                End If

                Return result
            End Function

            Friend Overrides Function GetCachedSlot(i As Integer) As SyntaxNode
                Dim result As SyntaxNode = Nothing

                If (i And 1) = 0 Then
                    'not a separator
                    Dim weak = Me._children(i >> 1).Value
                    If weak IsNot Nothing Then
                        weak.TryGetTarget(result)
                    End If
                End If

                Return result
            End Function
        End Class
    End Class
End Namespace
