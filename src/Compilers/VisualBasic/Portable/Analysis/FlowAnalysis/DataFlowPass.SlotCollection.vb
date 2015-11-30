' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class DataFlowPass

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
                    If _builder IsNot Nothing Then
                        Return _builder.Count
                    End If
                    Return If(_singleValue = SlotKind.Unreachable, 0, 1)
                End Get
            End Property

            Default Public Property Item(index As Integer) As Integer
                Get
                    Debug.Assert(index >= 0 AndAlso index < Count)
                    If _builder IsNot Nothing Then
                        Return _builder(index)
                    End If
                    Return _singleValue
                End Get
                Set(value As Integer)
                    Debug.Assert(index >= 0 AndAlso index < Count)
                    Debug.Assert(value <> SlotKind.Unreachable)
                    If _builder IsNot Nothing Then
                        _builder(index) = value
                    End If
                    _singleValue = value
                End Set
            End Property

            Public Sub Append(slot As Integer)
                Debug.Assert(slot <> SlotKind.Unreachable)
                If _builder IsNot Nothing Then
                    _builder.Add(slot)

                ElseIf _singleValue = 0 Then
                    _singleValue = slot

                Else
                    _builder = ArrayBuilder(Of Integer).GetInstance()
                    _builder.Add(_singleValue)
                    _builder.Add(slot)
                End If
            End Sub

            Public Sub Free()
                If _builder IsNot Nothing Then
                    _builder.Free()
                    _builder = Nothing
                End If
                _singleValue = 0
            End Sub
        End Structure

    End Class

End Namespace
