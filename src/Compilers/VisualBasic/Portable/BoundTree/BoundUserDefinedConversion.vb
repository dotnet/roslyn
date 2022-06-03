' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundUserDefinedConversion

        Public ReadOnly Property Operand As BoundExpression
            Get
                If (InOutConversionFlags And 1) <> 0 Then
                    Return DirectCast([Call].Arguments(0), BoundConversion).Operand
                Else
                    Return [Call].Arguments(0)
                End If
            End Get
        End Property

        Public ReadOnly Property InConversionOpt As BoundConversion
            Get
                If (InOutConversionFlags And 1) <> 0 Then
                    Return DirectCast([Call].Arguments(0), BoundConversion)
                End If

                Return Nothing
            End Get
        End Property

        Public ReadOnly Property OutConversionOpt As BoundConversion
            Get
                If (InOutConversionFlags And 2) <> 0 Then
                    Return DirectCast(UnderlyingExpression, BoundConversion)
                End If

                Return Nothing
            End Get
        End Property

        Public ReadOnly Property [Call] As BoundCall
            Get
                If (InOutConversionFlags And 2) <> 0 Then
                    Return DirectCast(DirectCast(UnderlyingExpression, BoundConversion).Operand, BoundCall)
                End If

                Return DirectCast(UnderlyingExpression, BoundCall)
            End Get
        End Property

#If DEBUG Then
        Private Sub Validate()

            Dim outConversion As BoundConversion = OutConversionOpt
            If outConversion IsNot Nothing Then
                Debug.Assert(Conversions.ConversionExists(outConversion.ConversionKind) AndAlso (outConversion.ConversionKind And ConversionKind.UserDefined) = 0)
            End If

            Dim underlyingCall = [Call]
            Debug.Assert(underlyingCall.Method.MethodKind = MethodKind.Conversion AndAlso underlyingCall.Method.ParameterCount = 1)

            Dim operand As BoundExpression
            Dim inConversion As BoundConversion = InConversionOpt

            If inConversion IsNot Nothing Then
                Debug.Assert(Conversions.ConversionExists(inConversion.ConversionKind) AndAlso (inConversion.ConversionKind And ConversionKind.UserDefined) = 0)
                operand = inConversion.Operand
            Else
                operand = underlyingCall.Arguments(0)
            End If

            Debug.Assert(operand.Type.IsSameTypeIgnoringAll(Type))
        End Sub
#End If

    End Class

End Namespace
