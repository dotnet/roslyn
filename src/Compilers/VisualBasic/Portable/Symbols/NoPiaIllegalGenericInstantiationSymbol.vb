' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A NoPiaIllegalGenericInstantiationSymbol is a special kind of ErrorSymbol that represents
    ''' a generic type instantiation that cannot cross assembly boundaries according to NoPia rules.
    ''' </summary>
    Friend Class NoPiaIllegalGenericInstantiationSymbol
        Inherits ErrorTypeSymbol

        Private ReadOnly m_UnderlyingSymbol As NamedTypeSymbol

        Public Sub New(underlyingSymbol As NamedTypeSymbol)
            m_UnderlyingSymbol = underlyingSymbol
        End Sub

        Public ReadOnly Property UnderlyingSymbol As NamedTypeSymbol
            Get
                Return m_UnderlyingSymbol
            End Get
        End Property

        Friend Overrides ReadOnly Property MangleName As Boolean
            Get
                Debug.Assert(Arity = 0)
                Return False
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return RuntimeHelpers.GetHashCode(Me)
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return obj Is Me
        End Function

        Friend Overrides ReadOnly Property ErrorInfo As DiagnosticInfo
            Get
                If m_UnderlyingSymbol.IsErrorType() Then
                    Dim underlyingInfo As DiagnosticInfo = DirectCast(m_UnderlyingSymbol, ErrorTypeSymbol).ErrorInfo

                    If underlyingInfo IsNot Nothing Then
                        Return underlyingInfo
                    End If
                End If

                Return ErrorFactory.ErrorInfo(ERRID.ERR_CannotUseGenericTypeAcrossAssemblyBoundaries, m_UnderlyingSymbol)
            End Get
        End Property
    End Class

End Namespace