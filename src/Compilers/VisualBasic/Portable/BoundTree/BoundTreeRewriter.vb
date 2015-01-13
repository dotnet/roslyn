' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend MustInherit Class BoundTreeRewriter
        Inherits BoundTreeVisitor

        Public Overridable Function VisitType(type As TypeSymbol) As TypeSymbol
            Return type
        End Function

        Public Overridable Function VisitList(Of T As BoundNode)(list As ImmutableArray(Of T)) As ImmutableArray(Of T)
            Dim newList As ArrayBuilder(Of T) = Nothing
            Dim i As Integer = 0
            Dim n As Integer = If(list.IsDefault, 0, list.Length)

            While (i < n)
                Dim item As T = list.Item(i)
                Debug.Assert(item IsNot Nothing)

                Dim visited = Me.Visit(item)

                If item IsNot visited AndAlso newList Is Nothing Then
                    newList = ArrayBuilder(Of T).GetInstance
                    If i > 0 Then
                        newList.AddRange(list, i)
                    End If
                End If

                If newList IsNot Nothing AndAlso visited IsNot Nothing Then
                    newList.Add(DirectCast(visited, T))
                End If

                i += 1
            End While

            If newList IsNot Nothing Then
                Return newList.ToImmutableAndFree
            Else
                Return list
            End If
        End Function

        Public Sub VisitList(Of T As BoundNode)(list As ImmutableArray(Of T), results As ArrayBuilder(Of T))
            For Each item In list
                results.Add(DirectCast(Visit(item), T))
            Next
        End Sub

    End Class
End Namespace