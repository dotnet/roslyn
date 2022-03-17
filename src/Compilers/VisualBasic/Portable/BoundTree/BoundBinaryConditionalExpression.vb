' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundBinaryConditionalExpression

#If DEBUG Then
        Private Sub Validate()
            ValidateConstantValue()

            If ConvertedTestExpression Is Nothing Then
                Debug.Assert(TestExpressionPlaceholder Is Nothing)
            ElseIf ConvertedTestExpression.Kind <> BoundKind.Conversion Then
                Debug.Assert(HasErrors)
                Debug.Assert(ConvertedTestExpression.Kind = BoundKind.BadExpression)
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
                        Dim conversion As ConversionKind = Conversions.ClassifyDirectCastConversion(TestExpression.Type, Type, CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
                        Debug.Assert(Conversions.IsWideningConversion(conversion) AndAlso Conversions.IsCLRPredefinedConversion(conversion))
                    End If
                End If
            End If
        End Sub
#End If

    End Class

End Namespace
