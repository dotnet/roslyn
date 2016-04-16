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

        ''' <summary> Represents variable symbol combined with the containing variable slot </summary>
        Protected Structure VariableIdentifier
            Implements IEquatable(Of VariableIdentifier)

            Public Shared None As VariableIdentifier = New VariableIdentifier()

            Public Sub New(symbol As Symbol, containingSlot As Integer)
                Debug.Assert(symbol IsNot Nothing)
                Me.Symbol = symbol
                Me.ContainingSlot = containingSlot
            End Sub

            Public ReadOnly Symbol As Symbol

            Public ReadOnly ContainingSlot As Integer

            Public ReadOnly Property Exists As Boolean
                Get
                    Return Symbol IsNot Nothing
                End Get
            End Property

            Public Overrides Function GetHashCode() As Integer
                Return Hash.Combine(Me.Symbol.GetHashCode, Me.ContainingSlot.GetHashCode)
            End Function

            Public Overloads Function Equals(obj As VariableIdentifier) As Boolean Implements IEquatable(Of VariableIdentifier).Equals
                Return Me.Symbol.Equals(obj.Symbol) AndAlso Me.ContainingSlot = obj.ContainingSlot
            End Function

            Public Overrides Function Equals(obj As Object) As Boolean
                Debug.Assert(obj IsNot Nothing)
                Dim other = DirectCast(obj, VariableIdentifier?)
                Return Equals(other.Value)
            End Function

            Public Shared Operator =(left As VariableIdentifier, right As VariableIdentifier) As Boolean
                Return left.Equals(right)
            End Operator

            Public Shared Operator <>(left As VariableIdentifier, right As VariableIdentifier) As Boolean
                Return Not left.Equals(right)
            End Operator

        End Structure

    End Class

End Namespace
