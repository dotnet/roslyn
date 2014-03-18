' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
'-----------------------------------------------------------------------------
' Contains various helpers used by preprocessor.
'-----------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Class TypeHelpers
        Private Sub New()
            ' do not create
        End Sub

        Friend Shared Function IsNumericType(tc As TypeCode) As Boolean
            Select Case tc
                Case TypeCode.Byte, TypeCode.Decimal, TypeCode.Double, TypeCode.Int16, TypeCode.Int32, TypeCode.Int64,
                    TypeCode.SByte, TypeCode.Single, TypeCode.UInt16, TypeCode.UInt32, TypeCode.UInt64

                    Return True
                Case Else
                    Return False
            End Select
            Return True
        End Function

        Friend Shared Function IsIntegralType(tc As TypeCode) As Boolean
            Select Case tc
                Case TypeCode.Byte, TypeCode.Int16, TypeCode.Int32, TypeCode.Int64,
                    TypeCode.SByte, TypeCode.UInt16, TypeCode.UInt32, TypeCode.UInt64

                    Return True
                Case Else
                    Return False
            End Select
            Return True
        End Function

        Friend Shared Function IsUnsignedIntegralType(tc As TypeCode) As Boolean
            Select Case tc
                Case TypeCode.SByte, TypeCode.UInt16, TypeCode.UInt32, TypeCode.UInt64

                    Return True
                Case Else
                    Return False
            End Select
            Return True
        End Function

        Friend Shared Function IsFloatingType(tc As TypeCode) As Boolean
            Select Case tc
                Case TypeCode.Double, TypeCode.Single

                    Return True
                Case Else
                    Return False
            End Select
            Return True
        End Function

        ' TODO: figure how to do this in VB.

#Region "Unchecked"

        Friend Shared Function UncheckedCLng(v As CConst) As Long
            Dim tc = v.TypeCode

            If IsIntegralType(tc) Then
                Return CType(v.ValueAsObject, Long)
            End If

            If tc = TypeCode.Char Then
                Return CLng(AscW(CChar(v.ValueAsObject)))
            End If

            If tc = TypeCode.DateTime Then
                Return CDate(v.ValueAsObject).ToBinary
            End If

            Throw ExceptionUtilities.UnexpectedValue(tc)
        End Function

#End Region

        Friend Shared Function VarDecAdd(
            pdecLeft As Decimal,
            pdecRight As Decimal,
            ByRef pdecResult As Decimal
        ) As Boolean
            Try
                pdecResult = Decimal.Add(pdecLeft, pdecRight)
            Catch ex As OverflowException
                Return True
            End Try
            Return False
        End Function

        Friend Shared Function VarDecSub(
            pdecLeft As Decimal,
            pdecRight As Decimal,
            ByRef pdecResult As Decimal
        ) As Boolean
            Try
                pdecResult = Decimal.Subtract(pdecLeft, pdecRight)
            Catch ex As OverflowException
                Return True
            End Try
            Return False
        End Function

        Friend Shared Function VarDecMul(
            pdecLeft As Decimal,
            pdecRight As Decimal,
            ByRef pdecResult As Decimal
        ) As Boolean
            Try
                pdecResult = Decimal.Multiply(pdecLeft, pdecRight)
            Catch ex As OverflowException
                Return True
            End Try
            Return False
        End Function

        Friend Shared Function VarDecDiv(
            pdecLeft As Decimal,
            pdecRight As Decimal,
            ByRef pdecResult As Decimal
        ) As Boolean
            Try
                pdecResult = Decimal.Divide(pdecLeft, pdecRight)
            Catch ex As OverflowException
                Return True
            End Try
            Return False
        End Function

        Friend Shared Function GetShiftSizeMask(Type As TypeCode) As Integer
            Select Case Type
                Case TypeCode.SByte, TypeCode.Byte
                    Return &H7

                Case TypeCode.Int16, TypeCode.UInt16
                    Return &HF

                Case TypeCode.Int32, TypeCode.UInt32
                    Return &H1F

                Case TypeCode.Int64, TypeCode.UInt64
                    Return &H3F

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(Type)
            End Select
        End Function
    End Class
End Namespace
