' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Public Overloads Function Equals(other As TypeWithModifiers) As Boolean Implements IEquatable(Of TypeWithModifiers).Equals
            Return Me.IsSameType(other, TypeCompareKind.ConsiderEverything)
        End Function

        Friend Function IsSameType(other As TypeWithModifiers, compareKind As TypeCompareKind) As Boolean
            If Not Me.Type.IsSameType(other.Type, compareKind) Then
                Return False
            End If

            If (compareKind And TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) = 0 Then
                Return If(Me.CustomModifiers.IsDefault,
                      other.CustomModifiers.IsDefault,
                      Not other.CustomModifiers.IsDefault AndAlso Me.CustomModifiers.SequenceEqual(other.CustomModifiers))
            End If

            Return True
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

        Public Function [Is](other As TypeSymbol) As Boolean
            Return TypeSymbol.Equals(Me.Type, other, TypeCompareKind.ConsiderEverything) AndAlso Me.CustomModifiers.IsEmpty
        End Function

        <Obsolete("Use Is method.", True)>
        Public Overloads Function Equals(other As TypeSymbol) As Boolean
            Return Me.Is(other)
        End Function

        ''' <summary>
        ''' Extract type under assumption that there should be no custom modifiers.
        ''' The method asserts otherwise.
        ''' </summary>
        ''' <returns></returns>
        Public Function AsTypeSymbolOnly() As TypeSymbol
            Debug.Assert(Me.CustomModifiers.IsEmpty)
            Return Me.Type
        End Function

        Public Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers
            Dim newCustomModifiers = If(substitution IsNot Nothing, substitution.SubstituteCustomModifiers(Me.CustomModifiers), Me.CustomModifiers)
            Dim newTypeWithModifiers As TypeWithModifiers = Me.Type.InternalSubstituteTypeParameters(substitution)
            If Not newTypeWithModifiers.Is(Me.Type) OrElse newCustomModifiers <> Me.CustomModifiers Then
                Return New TypeWithModifiers(newTypeWithModifiers.Type, newCustomModifiers.Concat(newTypeWithModifiers.CustomModifiers))
            Else
                Return Me ' substitution had no effect on the type or modifiers
            End If
        End Function
    End Structure
End Namespace
