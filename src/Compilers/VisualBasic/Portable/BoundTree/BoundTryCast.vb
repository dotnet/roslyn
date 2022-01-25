' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundTryCast

        Public Sub New(
            syntax As SyntaxNode,
            operand As BoundExpression,
            conversionKind As ConversionKind,
            type As TypeSymbol,
            Optional hasErrors As Boolean = False
        )
            Me.New(syntax, operand, conversionKind, constantValueOpt:=Nothing, type:=type, hasErrors:=hasErrors)
        End Sub

        Public Sub New(
            syntax As SyntaxNode,
            operand As BoundExpression,
            conversionKind As ConversionKind,
            relaxationLambdaOpt As BoundLambda,
            type As TypeSymbol,
            Optional hasErrors As Boolean = False
        )
            Me.New(syntax, operand, conversionKind, constantValueOpt:=Nothing, relaxationLambdaOpt:=relaxationLambdaOpt, type:=type, hasErrors:=hasErrors)
        End Sub

        Public Sub New(syntax As SyntaxNode, operand As BoundExpression, conversionKind As ConversionKind, constantValueOpt As ConstantValue, type As TypeSymbol, Optional hasErrors As Boolean = False)
            Me.New(syntax, operand, conversionKind, constantValueOpt, relaxationLambdaOpt:=Nothing, type:=type, hasErrors:=hasErrors)
        End Sub

        Public Overrides ReadOnly Property ExplicitCastInCode As Boolean
            Get
                Return True
            End Get
        End Property

#If DEBUG Then
        Private Sub Validate()
            ValidateConstantValue()
            Operand.AssertRValue()
        End Sub
#End If

    End Class
End Namespace
