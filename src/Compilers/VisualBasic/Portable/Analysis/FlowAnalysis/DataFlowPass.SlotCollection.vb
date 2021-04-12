' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class DataFlowPass

        ''' <summary>
        ''' Collection of 0, 1 or more slots. Allows returning of several slots by some 
        ''' DataFlowPass methods to handle cases where implicit receiver represents 
        ''' several variables, like in:
        '''     Dim a, b, c As New C(...) With {...}
        ''' 
        ''' Because such constructions are very rare in real user code, the collection only 
        ''' allocates an array builder for storing several values if there are indeed more 
        ''' than one slot to be stored. Because the collection may optionally create an 
        ''' array builder, collection's Free() method must be called when appropriate.
        ''' 
        ''' Note that the collection is mutable, so one can add or modify the values.
        ''' If some collection elements get replaced with 'SlotKind.NotTracked' collection
        ''' does NOT "shrink", i.e. once allocated the array builder is not freed even if 
        ''' all the elements are set to 'SlotKind.NotTracked'.
        ''' 
        ''' Collection cannot store 'SlotKind.Unreachable' (0) which has a special meaning.
        ''' </summary>
        Protected Structure SlotCollection
            Private _singleValue As Integer
            Private _builder As ArrayBuilder(Of Integer)

            Public ReadOnly Property Count As Integer
                Get
                    If Me._builder IsNot Nothing Then
                        Return Me._builder.Count
                    End If
                    Return If(Me._singleValue = SlotKind.Unreachable, 0, 1)
                End Get
            End Property

            Default Public Property Item(index As Integer) As Integer
                Get
                    Debug.Assert(index >= 0 AndAlso index < Me.Count)
                    If Me._builder IsNot Nothing Then
                        Return Me._builder(index)
                    End If
                    Return Me._singleValue
                End Get
                Set(value As Integer)
                    Debug.Assert(index >= 0 AndAlso index < Me.Count)
                    Debug.Assert(value <> SlotKind.Unreachable)
                    If Me._builder IsNot Nothing Then
                        Me._builder(index) = value
                    End If
                    Me._singleValue = value
                End Set
            End Property

            Public Sub Append(slot As Integer)
                Debug.Assert(slot <> SlotKind.Unreachable)
                If Me._builder IsNot Nothing Then
                    Me._builder.Add(slot)

                ElseIf Me._singleValue = 0 Then
                    Me._singleValue = slot

                Else
                    Me._builder = ArrayBuilder(Of Integer).GetInstance()
                    Me._builder.Add(Me._singleValue)
                    Me._builder.Add(slot)
                End If
            End Sub

            Public Sub Free()
                If Me._builder IsNot Nothing Then
                    Me._builder.Free()
                    Me._builder = Nothing
                End If
                Me._singleValue = 0
            End Sub
        End Structure

    End Class

End Namespace
