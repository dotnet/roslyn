' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    ''' <summary>
    ''' Base class of a compile time constant.
    ''' </summary>
    Friend MustInherit Class CConst
        Protected ReadOnly _errid As ERRID
        Protected ReadOnly _diagnosticArguments As Object()

        Public Sub New()
        End Sub

        Public Sub New(id As ERRID, ParamArray diagnosticArguments As Object())
            DiagnosticInfo.AssertMessageSerializable(diagnosticArguments)

            _errid = id
            _diagnosticArguments = diagnosticArguments
        End Sub

        Public MustOverride ReadOnly Property SpecialType As SpecialType
        Public MustOverride ReadOnly Property ValueAsObject As Object
        Public MustOverride Function WithError(id As ERRID) As CConst

        Friend Shared Function CreateChecked(value As Object) As CConst
            Dim constant = TryCreate(value)
            Debug.Assert(constant IsNot Nothing)
            Return constant
        End Function

        Friend Shared Function TryCreate(value As Object) As CConst
            If value Is Nothing Then
                Return CreateNothing()
            End If

            Dim specialType = SpecialTypeExtensions.FromRuntimeTypeOfLiteralValue(value)
            Select Case specialType
                Case SpecialType.System_Boolean
                    Return Create(Convert.ToBoolean(value))
                Case SpecialType.System_Byte
                    Return Create(Convert.ToByte(value))
                Case SpecialType.System_Char
                    Return Create(Convert.ToChar(value))
                Case SpecialType.System_DateTime
                    Return Create(Convert.ToDateTime(value))
                Case SpecialType.System_Decimal
                    Return Create(Convert.ToDecimal(value))
                Case SpecialType.System_Double
                    Return Create(Convert.ToDouble(value))
                Case SpecialType.System_Int16
                    Return Create(Convert.ToInt16(value))
                Case SpecialType.System_Int32
                    Return Create(Convert.ToInt32(value))
                Case SpecialType.System_Int64
                    Return Create(Convert.ToInt64(value))
                Case SpecialType.System_SByte
                    Return Create(Convert.ToSByte(value))
                Case SpecialType.System_Single
                    Return Create(Convert.ToSingle(value))
                Case SpecialType.System_String
                    Return Create(Convert.ToString(value))
                Case SpecialType.System_UInt16
                    Return Create(Convert.ToUInt16(value))
                Case SpecialType.System_UInt32
                    Return Create(Convert.ToUInt32(value))
                Case SpecialType.System_UInt64
                    Return Create(Convert.ToUInt64(value))
                Case Else
                    Return Nothing
            End Select
        End Function

        Friend Shared Function CreateNothing() As CConst(Of Object)
            Return New CConst(Of Object)(Nothing, SpecialType.System_Object)
        End Function

        Friend Shared Function Create(value As Boolean) As CConst(Of Boolean)
            Return New CConst(Of Boolean)(value, SpecialType.System_Boolean)
        End Function

        Friend Shared Function Create(value As Byte) As CConst(Of Byte)
            Return New CConst(Of Byte)(value, SpecialType.System_Byte)
        End Function

        Friend Shared Function Create(value As SByte) As CConst(Of SByte)
            Return New CConst(Of SByte)(value, SpecialType.System_SByte)
        End Function

        Friend Shared Function Create(value As Char) As CConst(Of Char)
            Return New CConst(Of Char)(value, SpecialType.System_Char)
        End Function

        Friend Shared Function Create(value As Short) As CConst(Of Short)
            Return New CConst(Of Short)(value, SpecialType.System_Int16)
        End Function

        Friend Shared Function Create(value As UShort) As CConst(Of UShort)
            Return New CConst(Of UShort)(value, SpecialType.System_UInt16)
        End Function

        Friend Shared Function Create(value As Integer) As CConst(Of Integer)
            Return New CConst(Of Integer)(value, SpecialType.System_Int32)
        End Function

        Friend Shared Function Create(value As UInteger) As CConst(Of UInteger)
            Return New CConst(Of UInteger)(value, SpecialType.System_UInt32)
        End Function

        Friend Shared Function Create(value As Long) As CConst(Of Long)
            Return New CConst(Of Long)(value, SpecialType.System_Int64)
        End Function

        Friend Shared Function Create(value As ULong) As CConst(Of ULong)
            Return New CConst(Of ULong)(value, SpecialType.System_UInt64)
        End Function

        Friend Shared Function Create(value As Decimal) As CConst(Of Decimal)
            Return New CConst(Of Decimal)(value, SpecialType.System_Decimal)
        End Function

        Friend Shared Function Create(value As String) As CConst(Of String)
            Return New CConst(Of String)(value, SpecialType.System_String)
        End Function

        Friend Shared Function Create(value As Single) As CConst(Of Single)
            Return New CConst(Of Single)(value, SpecialType.System_Single)
        End Function

        Friend Shared Function Create(value As Double) As CConst(Of Double)
            Return New CConst(Of Double)(value, SpecialType.System_Double)
        End Function

        Friend Shared Function Create(value As Date) As CConst(Of Date)
            Return New CConst(Of Date)(value, SpecialType.System_DateTime)
        End Function

        Public ReadOnly Property IsBad As Boolean
            Get
                Return SpecialType = SpecialType.None
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
                Return If(_diagnosticArguments, Array.Empty(Of Object))
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Represents a compile time constant.
    ''' </summary>
    Friend Class CConst(Of T)
        Inherits CConst

        Private ReadOnly _specialType As SpecialType
        Private ReadOnly _value As T

        Friend Sub New(value As T, specialType As SpecialType)
            _value = value
            _specialType = specialType
        End Sub

        Private Sub New(value As T, specialType As SpecialType, id As ERRID)
            MyBase.New(id)

            _value = value
            _specialType = specialType
        End Sub

        Public Overrides ReadOnly Property SpecialType As SpecialType
            Get
                Return _specialType
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
            Return New CConst(Of T)(_value, _specialType, id)
        End Function
    End Class

    Friend Class BadCConst
        Inherits CConst

        Public Sub New(id As ERRID)
            MyBase.New(id)
        End Sub

        Public Sub New(id As ERRID, ParamArray args As Object())
            MyBase.New(id, args)
        End Sub

        Public Overrides ReadOnly Property SpecialType As SpecialType
            Get
                Return SpecialType.None
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
