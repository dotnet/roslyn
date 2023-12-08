' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundBinaryOperator

        Public Sub New(
            syntax As SyntaxNode,
            operatorKind As BinaryOperatorKind,
            left As BoundExpression,
            right As BoundExpression,
            checked As Boolean,
            type As TypeSymbol,
            Optional hasErrors As Boolean = False
        )
            Me.New(syntax, operatorKind, left, right, checked, constantValueOpt:=Nothing, type:=type, hasErrors:=hasErrors)

        End Sub

#If DEBUG Then
        Private Sub Validate()
            ValidateConstantValue()
            Left.AssertRValue()
            Right.AssertRValue()
            Debug.Assert(HasErrors OrElse Left.Type.IsSameTypeIgnoringAll(Right.Type) OrElse
                         (OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.LeftShift OrElse
                         (OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.RightShift OrElse
                         (OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.Is OrElse
                         (OperatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.IsNot)
        End Sub
#End If

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                If (OperatorKind And BinaryOperatorKind.Error) = 0 Then
                    Dim op As BinaryOperatorKind = (OperatorKind And BinaryOperatorKind.OpMask)
                    Dim leftType = TryCast(Left.Type.GetNullableUnderlyingTypeOrSelf(), NamedTypeSymbol)

                    If leftType IsNot Nothing Then
                        Dim isChecked = Checked AndAlso leftType.IsIntegralType() AndAlso
                            (op = BinaryOperatorKind.Multiply OrElse
                             op = BinaryOperatorKind.Add OrElse
                             op = BinaryOperatorKind.Subtract OrElse
                             op = BinaryOperatorKind.IntegerDivide)
                        Dim opName As String = OverloadResolution.TryGetOperatorName(OperatorKind, isChecked)

                        If opName IsNot Nothing Then
                            Return New SynthesizedIntrinsicOperatorSymbol(
                                leftType,
                                opName,
                                Right.Type.GetNullableUnderlyingTypeOrSelf(),
                                Type.GetNullableUnderlyingTypeOrSelf())
                        End If
                    End If
                End If

                Return Nothing
            End Get
        End Property
    End Class

End Namespace
