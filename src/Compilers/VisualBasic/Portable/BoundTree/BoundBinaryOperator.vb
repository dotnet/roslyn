' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundBinaryOperator

        Public Sub New(
            syntax As VisualBasicSyntaxNode,
            operatorKind As BinaryOperatorKind,
            left As BoundExpression,
            right As BoundExpression,
            checked As Boolean,
            type As TypeSymbol,
            Optional hasErrors As Boolean = False
        )
            Me.New(syntax, operatorKind, left, right, checked, ConstantValueOpt:=Nothing, type:=type, hasErrors:=hasErrors)

        End Sub

#If DEBUG Then
        Private Sub Validate()
            ValidateConstantValue()
            Left.AssertRValue()
            Right.AssertRValue()
            Debug.Assert(HasErrors OrElse Left.Type.IsSameTypeIgnoringCustomModifiers(Right.Type) OrElse
                         (OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.LeftShift OrElse
                         (OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.RightShift OrElse
                         (OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.Is OrElse
                         (OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.IsNot)
        End Sub
#End If

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                If (OperatorKind And BinaryOperatorKind.Error) = 0 Then
                    Dim opName As String = OverloadResolution.TryGetOperatorName(OperatorKind)

                    If opName IsNot Nothing Then
                        Dim op As BinaryOperatorKind = (OperatorKind And BinaryOperatorKind.OpMask)
                        Dim leftType = DirectCast(Left.Type.GetNullableUnderlyingTypeOrSelf(), NamedTypeSymbol)
                        Return New SynthesizedIntrinsicOperatorSymbol(leftType,
                                                                      opName,
                                                                      Right.Type.GetNullableUnderlyingTypeOrSelf(),
                                                                      Type.GetNullableUnderlyingTypeOrSelf(),
                                                                      Checked AndAlso leftType.IsIntegralType() AndAlso
                                                                          (op = BinaryOperatorKind.Multiply OrElse
                                                                           op = BinaryOperatorKind.Add OrElse
                                                                           op = BinaryOperatorKind.Subtract OrElse
                                                                           op = BinaryOperatorKind.IntegerDivide))
                    End If
                End If

                Return Nothing
            End Get
        End Property
    End Class

End Namespace
