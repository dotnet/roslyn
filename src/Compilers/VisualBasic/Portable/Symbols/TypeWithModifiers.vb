﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Structure TypeWithModifiers
        Implements IEquatable(Of TypeWithModifiers)

        Public ReadOnly Type As TypeSymbol
        Public ReadOnly CustomModifiers As ImmutableArray(Of CustomModifier)

        Public Sub New(type As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier))
            Debug.Assert(type IsNot Nothing)
            Me.Type = type
            Me.CustomModifiers = customModifiers.NullToEmpty
        End Sub

        Public Sub New(type As TypeSymbol)
            Debug.Assert(type IsNot Nothing)
            Me.Type = type
            Me.CustomModifiers = ImmutableArray(Of CustomModifier).Empty
        End Sub

        <Obsolete("Use the strongly typed overload.", True)>
        Public Overrides Function Equals(obj As Object) As Boolean
            Return TypeOf obj Is TypeWithModifiers AndAlso Equals(DirectCast(obj, TypeWithModifiers))
        End Function

        Overloads Function Equals(other As TypeWithModifiers) As Boolean Implements IEquatable(Of TypeWithModifiers).Equals
            Return Me.Type = other.Type AndAlso
                   If(Me.CustomModifiers.IsDefault,
                      other.CustomModifiers.IsDefault,
                      Not other.CustomModifiers.IsDefault AndAlso Me.CustomModifiers.SequenceEqual(other.CustomModifiers))
        End Function

        Shared Operator =(x As TypeWithModifiers, y As TypeWithModifiers) As Boolean
            Return x.Equals(y)
        End Operator

        Shared Operator <>(x As TypeWithModifiers, y As TypeWithModifiers) As Boolean
            Return Not x.Equals(y)
        End Operator

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(Me.Type, Hash.CombineValues(Me.CustomModifiers))
        End Function

        Function [Is](other As TypeSymbol) As Boolean
            Return Me.Type = other AndAlso Me.CustomModifiers.IsEmpty
        End Function

        <Obsolete("Use Is method.", True)>
        Overloads Function Equals(other As TypeSymbol) As Boolean
            Return Me.Is(other)
        End Function

        ''' <summary>
        ''' Extract type under assumption that there should be no custom modifiers.
        ''' The method asserts otherwise.
        ''' </summary>
        ''' <returns></returns>
        Function AsTypeSymbolOnly() As TypeSymbol
            Debug.Assert(Me.CustomModifiers.IsEmpty)
            Return Me.Type
        End Function

        Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers
            Dim newTypeWithModifiers As TypeWithModifiers = Me.Type.InternalSubstituteTypeParameters(substitution)
            If Not newTypeWithModifiers.Is(Me.Type) Then
                Return New TypeWithModifiers(newTypeWithModifiers.Type, Me.CustomModifiers.Concat(newTypeWithModifiers.CustomModifiers))
            Else
                Return Me ' substitution had no effect on the type
            End If
        End Function
    End Structure
End Namespace
