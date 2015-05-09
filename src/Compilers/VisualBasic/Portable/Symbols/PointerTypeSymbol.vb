' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
' Copyright (c)  Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' An error type symbol to represent a pointer type.
    ''' Pointer types are not supported by VB language, but internally
    ''' we need to be able to match them in signatures of methods 
    ''' imported from metadata.
    ''' </summary>
    Friend NotInheritable Class PointerTypeSymbol
        Inherits ErrorTypeSymbol

        Private ReadOnly _pointedAtType As TypeSymbol
        Private ReadOnly _customModifiers As ImmutableArray(Of CustomModifier)

        Public Sub New(pointedAtType As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier))
            Debug.Assert(pointedAtType IsNot Nothing)

            _pointedAtType = pointedAtType
            _customModifiers = customModifiers.NullToEmpty()
        End Sub

        Friend Overrides ReadOnly Property MangleName As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ErrorInfo As DiagnosticInfo
            Get
                Return ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedType1, String.Empty)
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            ' We don't want to blow the stack if we have a type like T***************...***,
            ' so we do not recurse until we have a non-pointer. 

            Dim indirections As Integer = 0
            Dim current As PointerTypeSymbol = Me
            Dim last As TypeSymbol

            Do
                indirections += 1
                last = current._pointedAtType
                current = TryCast(last, PointerTypeSymbol)
            Loop While current IsNot Nothing

            Return Hash.Combine(last, indirections)
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            If Me Is obj Then
                Return True
            End If

            Dim other = TryCast(obj, PointerTypeSymbol)
            Return other IsNot Nothing AndAlso other._pointedAtType.Equals(_pointedAtType) AndAlso other._customModifiers.SequenceEqual(_customModifiers)
        End Function

    End Class

End Namespace
