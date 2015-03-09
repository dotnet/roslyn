' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Partial Class DataFlowPass
        Inherits AbstractFlowPass(Of LocalState)

        Protected Overrides Function IntersectWith(ByRef self As LocalState, ByRef other As LocalState) As Boolean
            If self.Reachable = other.Reachable Then
                If self.Assigned.Capacity <> other.Assigned.Capacity Then
                    Me.Normalize(self)
                    Me.Normalize(other)
                End If
                Return IntersectBitArrays(self.Assigned, other.Assigned)

            ElseIf Not self.Reachable Then
                self = other.Clone()
                Return True

            Else
                Return False
            End If
        End Function

        Protected Overrides Sub UnionWith(ByRef self As LocalState, ByRef other As LocalState)
            If self.Assigned.Capacity <> other.Assigned.Capacity Then
                Normalize(self)
                Normalize(other)
            End If

            For slot = 0 To SlotKind.FirstAvailable - 1
                If other.Assigned(slot) Then self.Assigned(slot) = True
            Next
            For slot = SlotKind.FirstAvailable To self.Assigned.Capacity - 1
                If other.Assigned(slot) AndAlso Not self.Assigned(slot) Then
                    SetSlotAssigned(slot, self)
                End If
            Next
        End Sub

        Private Shared ReadOnly Property UnreachableBitsSet As BitVector
            Get
                Return BitVector.AllSet(1)
            End Get
        End Property

        ''' <summary>
        ''' Intersect bit arrays taking into account 'all bits set' flag
        ''' </summary>
        ''' <remarks>receiver will be changed as a result</remarks>
        Private Shared Function IntersectBitArrays(ByRef receiver As BitVector, other As BitVector) As Boolean
            ' NOTE: a state with 'unreachable' slot set to 'assigned' means 'all bits are set'
            If other(SlotKind.Unreachable) Then
                ' OTHER state has 'all bits set', thus, RECEIVER does not need to be changed
                Return False
            Else
                If receiver(SlotKind.Unreachable) Then
                    ' RECEIVER = OTHER
                    receiver = other.Clone()
                    Return True
                Else
                    ' both RECEIVER and OTHER have valid bitsets
                    Return receiver.IntersectWith(other)
                End If
            End If
        End Function

        ''' <summary>
        ''' Union bit arrays taking into account 'all bits set' flag
        ''' </summary>
        ''' <remarks>receiver will be changed as a result</remarks>
        Private Shared Sub UnionBitArrays(ByRef receiver As BitVector, other As BitVector)
            ' NOTE: a state with 'unreachable' slot set to 'assigned' means 'all bits are set'
            If receiver(SlotKind.Unreachable) Then
                ' RECEIVER state has 'all bits set', thus, it does not need to be changed
            Else
                If other(SlotKind.Unreachable) Then
                    ' set RECEIVER to 'all bits are set'
                    receiver = UnreachableBitsSet
                Else
                    ' both RECEIVER and OTHER are valid bitsets
                    receiver.UnionWith(other)
                End If
            End If
        End Sub

        Protected Sub Normalize(ByRef _state As LocalState)
            Dim oldNext As Integer = _state.Assigned.Capacity
            _state.Assigned.EnsureCapacity(nextVariableSlot)
            For i = oldNext To nextVariableSlot - 1
                Dim id As VariableIdentifier = Me.variableBySlot(i)
                If id.ContainingSlot >= SlotKind.FirstAvailable AndAlso _state.Assigned(id.ContainingSlot) Then
                    _state.Assign(i)
                End If
            Next
        End Sub

        Friend Structure LocalState
            Implements AbstractLocalState

            Friend Assigned As BitVector

            Friend Sub New(assigned As BitVector)
                Debug.Assert(Not assigned.IsNull)
                Me.Assigned = assigned
            End Sub

            ''' <summary>
            ''' Produce a duplicate of this flow analysis state.
            ''' </summary>
            ''' <returns></returns>
            Public Function Clone() As LocalState Implements AbstractFlowPass(Of LocalState).AbstractLocalState.Clone
                Return New LocalState(Me.Assigned.Clone())
            End Function

            Public Function IsAssigned(slot As Integer) As Boolean
                ' We use the first bit as an indication of the fact that state has 'all bits set'. 
                ' If the first bit is set, rest of the bits are just ignored and assumed to be set.
                Return (slot = SlotKind.NotTracked) OrElse Assigned(SlotKind.Unreachable) OrElse Assigned(slot)
            End Function

            Public Sub Assign(slot As Integer)
                If slot <> SlotKind.NotTracked Then
                    Me.Assigned(slot) = True
                End If
            End Sub

            Public Sub Unassign(slot As Integer)
                If slot <> SlotKind.NotTracked Then
                    Me.Assigned(slot) = False
                End If
            End Sub

            Public ReadOnly Property Reachable As Boolean
                Get
                    Return Me.Assigned.Capacity <= 0 OrElse Not Me.IsAssigned(SlotKind.Unreachable)
                End Get
            End Property

            Public ReadOnly Property FunctionAssignedValue As Boolean
                Get
                    Return IsAssigned(SlotKind.FunctionValue)
                End Get
            End Property

        End Structure

    End Class

End Namespace
