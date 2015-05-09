' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundConversion

        Public Sub New(
            syntax As VisualBasicSyntaxNode,
            operand As BoundExpression,
            conversionKind As ConversionKind,
            checked As Boolean,
            explicitCastInCode As Boolean,
            type As TypeSymbol,
            Optional hasErrors As Boolean = False
        )
            Me.New(syntax, operand, conversionKind, checked, explicitCastInCode, ConstantValueOpt:=Nothing, type:=type, hasErrors:=hasErrors)
        End Sub

        Public Sub New(
            syntax As VisualBasicSyntaxNode,
            operand As BoundExpression,
            conversionKind As ConversionKind,
            checked As Boolean,
            explicitCastInCode As Boolean,
            relaxationLambdaOpt As BoundLambda,
            type As TypeSymbol,
            Optional hasErrors As Boolean = False
        )
            Me.New(syntax, operand, conversionKind, checked, explicitCastInCode, ConstantValueOpt:=Nothing, ConstructorOpt:=Nothing,
                   relaxationLambdaOpt:=relaxationLambdaOpt, RelaxationReceiverPlaceholderOpt:=Nothing, type:=type, hasErrors:=hasErrors)
        End Sub

        Public Sub New(
            syntax As VisualBasicSyntaxNode,
            operand As BoundExpression,
            conversionKind As ConversionKind,
            checked As Boolean,
            explicitCastInCode As Boolean,
            relaxationLambdaOpt As BoundLambda,
            RelaxationReceiverPlaceholderOpt As BoundRValuePlaceholder,
            type As TypeSymbol,
            Optional hasErrors As Boolean = False
        )
            Me.New(syntax, operand, conversionKind, checked, explicitCastInCode, ConstantValueOpt:=Nothing, ConstructorOpt:=Nothing,
                   relaxationLambdaOpt:=relaxationLambdaOpt, RelaxationReceiverPlaceholderOpt:=RelaxationReceiverPlaceholderOpt, type:=type, hasErrors:=hasErrors)
        End Sub

        Public Sub New(
                syntax As VisualBasicSyntaxNode,
                operand As BoundExpression,
                conversionKind As ConversionKind,
                checked As Boolean,
                explicitCastInCode As Boolean,
                constantValueOpt As ConstantValue,
                type As TypeSymbol,
                Optional hasErrors As Boolean = False
            )
            Me.New(syntax, operand, conversionKind, checked, explicitCastInCode, constantValueOpt:=constantValueOpt, ConstructorOpt:=Nothing, type:=type, hasErrors:=hasErrors)
        End Sub


        Public Sub New(syntax As VisualBasicSyntaxNode, operand As BoundExpression, conversionKind As ConversionKind, checked As Boolean, explicitCastInCode As Boolean, constantValueOpt As ConstantValue, constructorOpt As MethodSymbol, type As TypeSymbol, Optional hasErrors As Boolean = False)
            Me.New(syntax, operand, conversionKind, checked, explicitCastInCode, constantValueOpt, constructorOpt,
                   RelaxationLambdaOpt:=Nothing, RelaxationReceiverPlaceholderOpt:=Nothing, type:=type, hasErrors:=hasErrors)
        End Sub

#If DEBUG Then
        Private Sub Validate()
            ValidateConstantValue()
            Operand.AssertRValue()

            If Conversions.NoConversion(ConversionKind) Then
                Debug.Assert(Operand.Kind <> BoundKind.UserDefinedConversion)
            Else
                Debug.Assert(((ConversionKind And VisualBasic.ConversionKind.UserDefined) <> 0) = (Operand.Kind = BoundKind.UserDefinedConversion))

                If Operand.Kind = BoundKind.UserDefinedConversion Then
                    Dim udc = DirectCast(Operand, BoundUserDefinedConversion)
                    Debug.Assert(udc.UnderlyingExpression.Type.IsSameTypeIgnoringCustomModifiers(Type))

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
