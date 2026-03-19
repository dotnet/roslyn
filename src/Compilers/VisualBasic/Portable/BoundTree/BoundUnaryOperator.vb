' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundUnaryOperator

        Public Sub New(
            syntax As SyntaxNode,
            operatorKind As UnaryOperatorKind,
            operand As BoundExpression,
            checked As Boolean,
            type As TypeSymbol,
            Optional hasErrors As Boolean = False
        )
            Me.New(syntax, operatorKind, operand, checked, constantValueOpt:=Nothing, type:=type, hasErrors:=hasErrors OrElse operand.HasErrors())
        End Sub

#If DEBUG Then
        Private Sub Validate()
            ValidateConstantValue()
            Operand.AssertRValue()
            Debug.Assert(HasErrors OrElse Type.IsSameTypeIgnoringAll(Operand.Type))
        End Sub
#End If

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                If (OperatorKind And UnaryOperatorKind.Error) = 0 Then
                    Dim op As UnaryOperatorKind = (OperatorKind And UnaryOperatorKind.OpMask)
                    Dim operandType = TryCast(Operand.Type.GetNullableUnderlyingTypeOrSelf(), NamedTypeSymbol)

                    If operandType IsNot Nothing Then
                        Dim isChecked = Checked AndAlso operandType.IsIntegralType() AndAlso op = UnaryOperatorKind.Minus
                        Dim opName As String = OverloadResolution.TryGetOperatorName(OperatorKind, isChecked)

                        If opName IsNot Nothing Then
                            Return New SynthesizedIntrinsicOperatorSymbol(
                                operandType,
                                opName,
                                Type.GetNullableUnderlyingTypeOrSelf())
                        End If
                    End If
                End If

                Return Nothing
            End Get
        End Property
    End Class
End Namespace
