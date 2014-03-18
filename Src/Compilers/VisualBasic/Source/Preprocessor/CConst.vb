' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
'-----------------------------------------------------------------------------
' Contains types that represent compile time constants.
'-----------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend MustInherit Class CConst
        Protected ReadOnly _errid As ERRID
        Protected ReadOnly _errargs As Object()

        Sub New()
        End Sub

        Sub New(id As ERRID, ParamArray args As Object())
            _errid = id
            _errargs = args
        End Sub

        Public MustOverride ReadOnly Property TypeCode As TypeCode
        Public MustOverride ReadOnly Property ValueAsObject As Object
        Public MustOverride Function WithError(id As ERRID) As CConst

        Friend Shared Function Create(value As Object) As CConst
            If value Is Nothing Then
                Return Create(Of Object)(Nothing)
            End If

            Dim tc = System.Type.GetTypeCode(value.GetType)
            Select Case tc
                Case TypeCode.Boolean
                    Return Create(Convert.ToBoolean(value))
                Case TypeCode.Byte
                    Return Create(Convert.ToByte(value))
                Case TypeCode.Char
                    Return Create(Convert.ToChar(value))
                Case TypeCode.DateTime
                    Return Create(Convert.ToDateTime(value))
                Case TypeCode.Decimal
                    Return Create(Convert.ToDecimal(value))
                Case TypeCode.Double
                    Return Create(Convert.ToDouble(value))
                Case TypeCode.Int16
                    Return Create(Convert.ToInt16(value))
                Case TypeCode.Int32
                    Return Create(Convert.ToInt32(value))
                Case TypeCode.Int64
                    Return Create(Convert.ToInt64(value))
                Case TypeCode.SByte
                    Return Create(Convert.ToSByte(value))
                Case TypeCode.Single
                    Return Create(Convert.ToSingle(value))
                Case TypeCode.String
                    Return Create(Convert.ToString(value))
                Case TypeCode.UInt16
                    Return Create(Convert.ToUInt16(value))
                Case TypeCode.UInt32
                    Return Create(Convert.ToUInt32(value))
                Case TypeCode.UInt64
                    Return Create(Convert.ToUInt64(value))
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(tc)
            End Select

        End Function

        Friend Shared Function Create(Of T)(value As T) As CConst(Of T)
            Return New CConst(Of T)(value)
        End Function

        Public ReadOnly Property IsBad As Boolean
            Get
                Return TypeCode = TypeCode.Empty
            End Get
        End Property

        Public ReadOnly Property IsBooleanTrue As Boolean
            Get
                If IsBad Then
                    Return False
                End If

                Dim boolValue = TryCast(Me, CConst(Of Boolean))
                If boolValue IsNot Nothing Then
                    Return boolValue.Value
                End If

                Return False
            End Get
        End Property

        Public ReadOnly Property ErrorId As ERRID
            Get
                Return _errid
            End Get
        End Property

        Public ReadOnly Property ErrorArgs As Object()
            Get
                Return If(_errargs, New Object() {})
            End Get
        End Property
    End Class

    Friend Class CConst(Of T)
        Inherits CConst

        Private ReadOnly _tc As TypeCode
        Private ReadOnly _value As T

        Friend Sub New(value As T)
            _value = value
            _tc = System.Type.GetTypeCode(GetType(T))
        End Sub

        Private Sub New(value As T, tc As TypeCode, id As ERRID)
            MyBase.New(id)

            _value = value
            _tc = tc
        End Sub

        Public Overrides ReadOnly Property TypeCode As TypeCode
            Get
                Return _tc
            End Get
        End Property

        Public Overrides ReadOnly Property ValueAsObject As Object
            Get
                Return _value
            End Get
        End Property

        Public ReadOnly Property Value As T
            Get
                Return _value
            End Get
        End Property

        Public Overrides Function WithError(id As ERRID) As CConst
            Return New CConst(Of T)(_value, _tc, id)
        End Function
    End Class

    Friend Class BadCConst
        Inherits CConst

        Sub New(id As ERRID, ParamArray args As Object())
            MyBase.New(id, args)
        End Sub

        Public Overrides ReadOnly Property TypeCode As TypeCode
            Get
                Return TypeCode.Empty
            End Get
        End Property

        Public Overrides ReadOnly Property ValueAsObject As Object
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function WithError(id As ERRID) As CConst
            ' TODO: we support only one error for now.
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class
End Namespace
