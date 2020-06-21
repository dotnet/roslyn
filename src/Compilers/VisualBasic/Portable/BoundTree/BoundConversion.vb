' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundConversion

        Public Sub New(
            syntax As SyntaxNode,
            operand As BoundExpression,
            conversionKind As ConversionKind,
            checked As Boolean,
            explicitCastInCode As Boolean,
            type As TypeSymbol,
            Optional hasErrors As Boolean = False
        )
            Me.New(syntax, operand, conversionKind, checked, explicitCastInCode, constantValueOpt:=Nothing, type:=type, hasErrors:=hasErrors)
        End Sub

        Public Sub New(
                syntax As SyntaxNode,
                operand As BoundExpression,
                conversionKind As ConversionKind,
                checked As Boolean,
                explicitCastInCode As Boolean,
                constantValueOpt As ConstantValue,
                type As TypeSymbol,
                Optional hasErrors As Boolean = False
            )
            Me.New(syntax, operand, conversionKind, checked, explicitCastInCode, constantValueOpt:=constantValueOpt, extendedInfoOpt:=Nothing, type:=type, hasErrors:=hasErrors)
        End Sub

#If DEBUG Then
        Private Sub Validate()
            ValidateConstantValue()
            Operand.AssertRValue()

            If Conversions.NoConversion(ConversionKind) Then
                Debug.Assert((ConversionKind And VisualBasic.ConversionKind.UserDefined) = 0)
            Else
                Debug.Assert(((ConversionKind And VisualBasic.ConversionKind.UserDefined) <> 0) = (Operand.Kind = BoundKind.UserDefinedConversion))

                If Conversions.IsIdentityConversion(ConversionKind) Then
                    Debug.Assert(ExtendedInfoOpt Is Nothing)
                End If

                If (ConversionKind And (ConversionKind.Lambda Or ConversionKind.AnonymousDelegate)) <> 0 Then
                    Debug.Assert(ExtendedInfoOpt Is Nothing OrElse ExtendedInfoOpt.Kind = BoundKind.RelaxationLambda)
                    Debug.Assert((ConversionKind And ConversionKind.AnonymousDelegate) <> 0 OrElse
                                 TryCast(ExtendedInfoOpt, BoundRelaxationLambda)?.ReceiverPlaceholderOpt Is Nothing)
                End If

                If (ConversionKind And ConversionKind.Tuple) <> 0 Then
                    If ExtendedInfoOpt Is Nothing Then
                        Debug.Assert(Operand.Kind = BoundKind.ConvertedTupleLiteral OrElse Operand.HasErrors)
                    Else
                        Debug.Assert(ExtendedInfoOpt.Kind = BoundKind.ConvertedTupleElements)
                    End If
                End If

                If Operand.Kind = BoundKind.UserDefinedConversion Then
                    Dim udc = DirectCast(Operand, BoundUserDefinedConversion)
                    Debug.Assert(udc.UnderlyingExpression.Type.IsSameTypeIgnoringAll(Type))

                    If (ConversionKind And VisualBasic.ConversionKind.Nullable) <> 0 Then
                        Dim underlyingCall As BoundCall = udc.Call
                        Debug.Assert(udc.Type.IsNullableType() AndAlso Not underlyingCall.Method.Parameters(0).Type.IsNullableType())
                    End If
                End If
            End If
        End Sub
#End If

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Dim method As MethodSymbol = Nothing

                If (ConversionKind And ConversionKind.UserDefined) <> 0 AndAlso
                   Operand.Kind = BoundKind.UserDefinedConversion Then
                    Dim expr As BoundExpression = DirectCast(Operand, BoundUserDefinedConversion).UnderlyingExpression

                    If expr.Kind = BoundKind.Conversion Then
                        expr = DirectCast(expr, BoundConversion).Operand
                    End If

                    If expr.Kind = BoundKind.Call Then
                        method = DirectCast(expr, BoundCall).Method
                    End If
                End If

                Return method
            End Get
        End Property
    End Class
End Namespace
