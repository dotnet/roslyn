﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundBinaryConditionalExpression

#If DEBUG Then
        Private Sub Validate()
            ValidateConstantValue()

            If ConvertedTestExpression Is Nothing Then
                Debug.Assert(TestExpressionPlaceholder Is Nothing)
            End If

            If Not HasErrors Then
                TestExpression.AssertRValue()
                ElseExpression.AssertRValue()

                Debug.Assert(ElseExpression.IsNothingLiteral() OrElse
                             (TestExpression.IsConstant AndAlso Not TestExpression.ConstantValueOpt.IsNothing) OrElse
                             Type.IsSameTypeIgnoringAll(ElseExpression.Type))

                If ConvertedTestExpression IsNot Nothing Then
                    ConvertedTestExpression.AssertRValue()
                    Debug.Assert(Type.IsSameTypeIgnoringAll(ConvertedTestExpression.Type))

                    If TestExpressionPlaceholder IsNot Nothing Then
                        Debug.Assert(TestExpressionPlaceholder.Type.IsSameTypeIgnoringAll(TestExpression.Type.GetNullableUnderlyingTypeOrSelf()))
                    End If
                Else
                    If Not Type.IsSameTypeIgnoringAll(TestExpression.Type.GetNullableUnderlyingTypeOrSelf()) Then
                        Dim conversion As ConversionKind = Conversions.ClassifyDirectCastConversion(TestExpression.Type, Type, Nothing)
                        Debug.Assert(Conversions.IsWideningConversion(conversion) AndAlso Conversions.IsCLRPredefinedConversion(conversion))
                    End If
                End If
            End If
        End Sub
#End If

    End Class

End Namespace
