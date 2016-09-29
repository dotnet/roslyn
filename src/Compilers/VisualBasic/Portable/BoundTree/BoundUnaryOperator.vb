' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            Debug.Assert(HasErrors OrElse Type.IsSameTypeIgnoringCustomModifiers(Operand.Type))
        End Sub
#End If

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                If (OperatorKind And UnaryOperatorKind.Error) = 0 Then
                    Dim opName As String = OverloadResolution.TryGetOperatorName(OperatorKind)

                    If opName IsNot Nothing Then
                        Dim op As UnaryOperatorKind = (OperatorKind And UnaryOperatorKind.OpMask)
                        Dim operandType = DirectCast(Operand.Type.GetNullableUnderlyingTypeOrSelf(), NamedTypeSymbol)
                        Return New SynthesizedIntrinsicOperatorSymbol(operandType,
                                                                      opName,
                                                                      Type.GetNullableUnderlyingTypeOrSelf(),
                                                                      Checked AndAlso operandType.IsIntegralType() AndAlso
                                                                          op = UnaryOperatorKind.Minus)
                    End If
                End If

                Return Nothing
            End Get
        End Property
    End Class
End Namespace