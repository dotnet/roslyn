' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        ' TODO: figure how to do this in VB.

#Region "Unchecked"

        Friend Shared Function UncheckedCLng(v As CConst) As Long
            Dim specialType = v.SpecialType

            If specialType.IsIntegralType() Then
                Return CType(v.ValueAsObject, Long)
            End If

            If specialType = SpecialType.System_Char Then
                Return AscW(CChar(v.ValueAsObject))
            End If

            If specialType = SpecialType.System_DateTime Then
                Return CDate(v.ValueAsObject).ToBinary
            End If

            Throw ExceptionUtilities.UnexpectedValue(specialType)
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
    End Class
End Namespace
