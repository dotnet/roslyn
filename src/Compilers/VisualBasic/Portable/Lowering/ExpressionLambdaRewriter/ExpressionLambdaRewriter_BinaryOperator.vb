' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class ExpressionLambdaRewriter

        Private Function VisitBinaryOperator(node As BoundBinaryOperator) As BoundExpression
            Debug.Assert((node.OperatorKind And BinaryOperatorKind.UserDefined) = 0)

            Select Case node.OperatorKind And BinaryOperatorKind.OpMask
                Case BinaryOperatorKind.And,
                     BinaryOperatorKind.Or,
                     BinaryOperatorKind.Xor,
                     BinaryOperatorKind.Power,
                     BinaryOperatorKind.Multiply,
                     BinaryOperatorKind.Add,
                     BinaryOperatorKind.Subtract,
                     BinaryOperatorKind.Divide,
                     BinaryOperatorKind.Modulo,
                     BinaryOperatorKind.IntegerDivide,
                     BinaryOperatorKind.LeftShift,
                     BinaryOperatorKind.RightShift
                    Return ConvertBinaryOperator(node)

                Case BinaryOperatorKind.Is,
                     BinaryOperatorKind.IsNot,
                     BinaryOperatorKind.Equals,
                     BinaryOperatorKind.NotEquals,
                     BinaryOperatorKind.LessThanOrEqual,
                     BinaryOperatorKind.GreaterThanOrEqual,
                     BinaryOperatorKind.LessThan,
                     BinaryOperatorKind.GreaterThan
                    Return ConvertBooleanOperator(node)

                Case BinaryOperatorKind.OrElse,
                     BinaryOperatorKind.AndAlso
                    Return ConvertShortCircuitedBooleanOperator(node)

                Case BinaryOperatorKind.Like,
                     BinaryOperatorKind.Concatenate
                    ' Should already be rewritten by this time
                    Throw ExceptionUtilities.UnexpectedValue(node.OperatorKind)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.OperatorKind)
            End Select
        End Function

        Private Function VisitUserDefinedBinaryOperator(node As BoundUserDefinedBinaryOperator) As BoundExpression
            Dim opKind As BinaryOperatorKind = node.OperatorKind And BinaryOperatorKind.OpMask
            Dim isLifted As Boolean = (node.OperatorKind And BinaryOperatorKind.Lifted) <> 0
            Dim isChecked As Boolean = node.Checked AndAlso IsIntegralType(node.Call.Method.ReturnType)

            Select Case opKind
                Case BinaryOperatorKind.Like,
                     BinaryOperatorKind.Concatenate
                    Return ConvertUserDefinedLikeOrConcate(node)

                Case BinaryOperatorKind.Is,
                     BinaryOperatorKind.IsNot,
                     BinaryOperatorKind.Equals,
                     BinaryOperatorKind.NotEquals,
                     BinaryOperatorKind.LessThanOrEqual,
                     BinaryOperatorKind.GreaterThanOrEqual,
                     BinaryOperatorKind.LessThan,
                     BinaryOperatorKind.GreaterThan

                    ' Error should have been reported by DiagnosticsPass, see 
                    '       DiagnosticsPass.VisitUserDefinedBinaryOperator
                    Debug.Assert(Not isLifted OrElse Not node.Call.Method.ReturnType.IsNullableType)

                    Return ConvertRuntimeHelperToExpressionTree(GetComparisonBinaryOperatorFactoryWithMethodInfo(opKind),
                                                                Visit(node.Left), Visit(node.Right),
                                                                Me._factory.Literal(isLifted),
                                                                _factory.MethodInfo(node.Call.Method, _factory.WellKnownType(WellKnownType.System_Reflection_MethodInfo)))

                Case Else
                    ' Error should have been reported by DiagnosticsPass, see 
                    '       DiagnosticsPass.VisitUserDefinedBinaryOperator
                    Debug.Assert(Not isLifted OrElse Not node.Call.Method.ReturnType.IsNullableType)

                    Return ConvertRuntimeHelperToExpressionTree(GetNonComparisonBinaryOperatorFactoryWithMethodInfo(opKind, isChecked),
                                                                Visit(node.Left), Visit(node.Right),
                                                                _factory.MethodInfo(node.Call.Method, _factory.WellKnownType(WellKnownType.System_Reflection_MethodInfo)))
            End Select
        End Function

        Private Shared Function GetComparisonBinaryOperatorFactoryWithMethodInfo(opKind As BinaryOperatorKind) As WellKnownMember
            Select Case opKind And BinaryOperatorKind.OpMask
                Case BinaryOperatorKind.Is,
                     BinaryOperatorKind.Equals
                    Return WellKnownMember.System_Linq_Expressions_Expression__Equal_MethodInfo

                Case BinaryOperatorKind.IsNot,
                     BinaryOperatorKind.NotEquals
                    Return WellKnownMember.System_Linq_Expressions_Expression__NotEqual_MethodInfo

                Case BinaryOperatorKind.LessThanOrEqual
                    Return WellKnownMember.System_Linq_Expressions_Expression__LessThanOrEqual_MethodInfo

                Case BinaryOperatorKind.GreaterThanOrEqual
                    Return WellKnownMember.System_Linq_Expressions_Expression__GreaterThanOrEqual_MethodInfo

                Case BinaryOperatorKind.LessThan
                    Return WellKnownMember.System_Linq_Expressions_Expression__LessThan_MethodInfo

                Case BinaryOperatorKind.GreaterThan
                    Return WellKnownMember.System_Linq_Expressions_Expression__GreaterThan_MethodInfo

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select
        End Function

        Private Shared Function GetNonComparisonBinaryOperatorFactoryWithMethodInfo(opKind As BinaryOperatorKind, isChecked As Boolean) As WellKnownMember
            Select Case opKind And BinaryOperatorKind.OpMask
                Case BinaryOperatorKind.Add
                    Return If(isChecked,
                              WellKnownMember.System_Linq_Expressions_Expression__AddChecked_MethodInfo,
                              WellKnownMember.System_Linq_Expressions_Expression__Add_MethodInfo)

                Case BinaryOperatorKind.Subtract
                    Return If(isChecked,
                              WellKnownMember.System_Linq_Expressions_Expression__SubtractChecked_MethodInfo,
                              WellKnownMember.System_Linq_Expressions_Expression__Subtract_MethodInfo)

                Case BinaryOperatorKind.Multiply
                    Return If(isChecked,
                              WellKnownMember.System_Linq_Expressions_Expression__MultiplyChecked_MethodInfo,
                              WellKnownMember.System_Linq_Expressions_Expression__Multiply_MethodInfo)

                Case BinaryOperatorKind.IntegerDivide,
                     BinaryOperatorKind.Divide
                    Return WellKnownMember.System_Linq_Expressions_Expression__Divide_MethodInfo

                Case BinaryOperatorKind.Modulo
                    Return WellKnownMember.System_Linq_Expressions_Expression__Modulo_MethodInfo

                Case BinaryOperatorKind.Power
                    Return WellKnownMember.System_Linq_Expressions_Expression__Power_MethodInfo

                Case BinaryOperatorKind.And
                    Return WellKnownMember.System_Linq_Expressions_Expression__And_MethodInfo

                Case BinaryOperatorKind.Or
                    Return WellKnownMember.System_Linq_Expressions_Expression__Or_MethodInfo

                Case BinaryOperatorKind.Xor
                    Return WellKnownMember.System_Linq_Expressions_Expression__ExclusiveOr_MethodInfo

                Case BinaryOperatorKind.LeftShift
                    Return WellKnownMember.System_Linq_Expressions_Expression__LeftShift_MethodInfo

                Case BinaryOperatorKind.RightShift
                    Return WellKnownMember.System_Linq_Expressions_Expression__RightShift_MethodInfo

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select
        End Function

        Private Function VisitUserDefinedShortCircuitingOperator(node As BoundUserDefinedShortCircuitingOperator) As BoundExpression
            Dim operand As BoundUserDefinedBinaryOperator = node.BitwiseOperator
            Dim opKind As BinaryOperatorKind = operand.OperatorKind And BinaryOperatorKind.OpMask

            ' See comment in DiagnosticsPass.VisitUserDefinedShortCircuitingOperator
            Debug.Assert(operand.Call.Method.ReturnType.IsSameTypeIgnoringAll(operand.Call.Method.Parameters(0).Type) AndAlso
                         operand.Call.Method.ReturnType.IsSameTypeIgnoringAll(operand.Call.Method.Parameters(1).Type))

            Return ConvertRuntimeHelperToExpressionTree(If(opKind = BinaryOperatorKind.And,
                                                           WellKnownMember.System_Linq_Expressions_Expression__AndAlso_MethodInfo,
                                                           WellKnownMember.System_Linq_Expressions_Expression__OrElse_MethodInfo),
                                                        Visit(operand.Left), Visit(operand.Right),
                                                        _factory.MethodInfo(operand.Call.Method, _factory.WellKnownType(WellKnownType.System_Reflection_MethodInfo)))
        End Function

#Region "User Defined Like"

        Private Function ConvertUserDefinedLikeOrConcate(node As BoundUserDefinedBinaryOperator) As BoundExpression
            Dim [call] As BoundCall = node.Call
            Dim opKind As BinaryOperatorKind = node.OperatorKind

            If (opKind And BinaryOperatorKind.Lifted) = 0 Then
                Return VisitInternal([call])
            End If

            Return VisitInternal(AdjustCallForLiftedOperator(opKind, [call], node.Type))
        End Function

#End Region

#Region "Is, IsNot, <, <=, >=, >"

        Private Function ConvertBooleanOperator(node As BoundBinaryOperator) As BoundExpression
            Dim resultType As TypeSymbol = node.Type

            Dim resultNotNullableType As TypeSymbol = resultType.GetNullableUnderlyingTypeOrSelf
            Dim resultUnderlyingType As TypeSymbol = resultNotNullableType.GetEnumUnderlyingTypeOrSelf
            Dim resultUnderlyingSpecialType As SpecialType = resultUnderlyingType.SpecialType

            Dim opKind = node.OperatorKind And BinaryOperatorKind.OpMask
            Debug.Assert(opKind = BinaryOperatorKind.Is OrElse opKind = BinaryOperatorKind.IsNot OrElse
                         opKind = BinaryOperatorKind.Equals OrElse opKind = BinaryOperatorKind.NotEquals OrElse
                         opKind = BinaryOperatorKind.LessThan OrElse opKind = BinaryOperatorKind.GreaterThan OrElse
                         opKind = BinaryOperatorKind.LessThanOrEqual OrElse opKind = BinaryOperatorKind.GreaterThanOrEqual)
            Debug.Assert((node.OperatorKind And BinaryOperatorKind.UserDefined) = 0)

            ' Prepare left and right
            Dim originalLeft As BoundExpression = node.Left
            Dim operandType As TypeSymbol = originalLeft.Type

            Dim left As BoundExpression = Nothing
            Dim originalRight As BoundExpression = node.Right
            Dim right As BoundExpression = Nothing

            Dim isIsIsNot As Boolean = (opKind = BinaryOperatorKind.Is) OrElse (opKind = BinaryOperatorKind.IsNot)

            If isIsIsNot Then
                ' Ensure Nothing literals is converted to the type of an opposite operand
                If originalLeft.IsNothingLiteral Then
                    If originalRight.Type.IsNullableType Then
                        Debug.Assert(originalLeft.Type Is Nothing OrElse originalLeft.Type.IsObjectType)
                        left = CreateLiteralExpression(originalLeft, originalRight.Type)
                        operandType = originalRight.Type
                    End If

                ElseIf originalRight.IsNothingLiteral Then
                    If originalLeft.Type.IsNullableType Then
                        Debug.Assert(originalRight.Type Is Nothing OrElse originalRight.Type.IsObjectType)
                        right = CreateLiteralExpression(originalRight, originalLeft.Type)
                    End If
                End If
            End If

            Dim operandIsNullable As Boolean = operandType.IsNullableType
            Dim operandNotNullableType As TypeSymbol = operandType.GetNullableUnderlyingTypeOrSelf
            Dim operandUnderlyingType As TypeSymbol = operandNotNullableType.GetEnumUnderlyingTypeOrSelf
            Dim operandUnderlyingSpecialType As SpecialType = operandUnderlyingType.SpecialType

            If left Is Nothing Then
                left = Visit(originalLeft)
                Debug.Assert(TypeSymbol.Equals(operandType, originalLeft.Type, TypeCompareKind.ConsiderEverything))
            End If

            If right Is Nothing Then
                right = Visit(originalRight)
                Debug.Assert(TypeSymbol.Equals(operandType, originalRight.Type, TypeCompareKind.ConsiderEverything))
            End If

            ' Do we need to handle special cases?
            Dim helper As MethodSymbol = Nothing

            ' [>|>=|<|<=] with System.Object argument should already be rewritten into proper calls
            Debug.Assert(operandUnderlyingSpecialType <> SpecialType.System_Object OrElse isIsIsNot)

            ' All boolean operators with System.String argument should already be rewritten into proper calls
            Debug.Assert(operandUnderlyingSpecialType <> SpecialType.System_String)

            If operandUnderlyingSpecialType = SpecialType.System_Decimal Then
                helper = GetHelperForDecimalBinaryOperation(opKind)
            ElseIf operandUnderlyingSpecialType = SpecialType.System_DateTime Then
                helper = GetHelperForDateTimeBinaryOperation(opKind)
            End If

            If helper IsNot Nothing Then
                Debug.Assert(helper.MethodKind = MethodKind.Ordinary OrElse helper.MethodKind = MethodKind.UserDefinedOperator)

                Return ConvertRuntimeHelperToExpressionTree(GetComparisonBinaryOperatorFactoryWithMethodInfo(opKind),
                                                            left, right, Me._factory.Literal(resultType.IsNullableType), _factory.MethodInfo(helper, _factory.WellKnownType(WellKnownType.System_Reflection_MethodInfo)))
            End If

            ' No helpers starting from here

            Dim convertOperandsToInteger As Boolean = False
            If operandUnderlyingSpecialType = SpecialType.System_Boolean Then
                ' For LT, LE, GT, and GE, if both the arguments are boolean arguments,
                ' we must generate a conversion from the boolean argument to an integer.

                ' Because True is -1, we need to switch the comparisons in these cases.
                Select Case opKind
                    Case BinaryOperatorKind.LessThan
                        opKind = BinaryOperatorKind.GreaterThan
                        convertOperandsToInteger = True

                    Case BinaryOperatorKind.LessThanOrEqual
                        opKind = BinaryOperatorKind.GreaterThanOrEqual
                        convertOperandsToInteger = True

                    Case BinaryOperatorKind.GreaterThan
                        opKind = BinaryOperatorKind.LessThan
                        convertOperandsToInteger = True

                    Case BinaryOperatorKind.GreaterThanOrEqual
                        opKind = BinaryOperatorKind.LessThanOrEqual
                        convertOperandsToInteger = True
                End Select
            End If

            Dim operandActiveType As TypeSymbol = operandType

            ' Convert arguments to underlying type if they are enums
            If operandNotNullableType.IsEnumType AndAlso Not isIsIsNot Then
                ' Assuming both operands are of the same type
                Dim newType As TypeSymbol = If(operandIsNullable, Me._factory.NullableOf(operandUnderlyingType), operandUnderlyingType)

                left = CreateBuiltInConversion(operandActiveType, newType, left, node.Checked, False, ConversionSemantics.[Default])
                right = CreateBuiltInConversion(operandActiveType, newType, right, node.Checked, False, ConversionSemantics.[Default])

                operandActiveType = newType
            End If

            ' Check if we need to convert the boolean arguments to Int32.
            If convertOperandsToInteger AndAlso Not isIsIsNot Then
                Dim newType As TypeSymbol = If(operandIsNullable, Me._factory.NullableOf(Me.Int32Type), Me.Int32Type)
                left = Convert(left, newType, node.Checked)
                right = Convert(right, newType, node.Checked)
                operandActiveType = newType
            End If

            Return ConvertRuntimeHelperToExpressionTree(GetComparisonBinaryOperatorFactoryWithMethodInfo(opKind),
                                                        left, right, Me._factory.Literal(resultType.IsNullableType),
                                                        Me._factory.Null(_factory.WellKnownType(WellKnownType.System_Reflection_MethodInfo)))
        End Function

#End Region

#Region "AndAlso, OrElse"

        Private Function ConvertShortCircuitedBooleanOperator(node As BoundBinaryOperator) As BoundExpression
            Dim resultType As TypeSymbol = node.Type

            Dim resultUnderlyingType As TypeSymbol = GetUnderlyingType(resultType)
            Dim resultUnderlyingSpecialType As SpecialType = resultUnderlyingType.SpecialType

            Dim opKind = node.OperatorKind And BinaryOperatorKind.OpMask
            Debug.Assert(opKind = BinaryOperatorKind.AndAlso OrElse opKind = BinaryOperatorKind.OrElse)
            Debug.Assert((node.OperatorKind And BinaryOperatorKind.UserDefined) = 0)

            Dim originalLeft As BoundExpression = node.Left
            Dim left As BoundExpression = Visit(originalLeft)
            Debug.Assert(TypeSymbol.Equals(resultType, originalLeft.Type, TypeCompareKind.ConsiderEverything))

            Dim originalRight As BoundExpression = node.Right
            Dim right As BoundExpression = Visit(originalRight)
            Debug.Assert(TypeSymbol.Equals(resultType, originalRight.Type, TypeCompareKind.ConsiderEverything))

            If resultUnderlyingType.IsObjectType Then
                Dim systemBool As TypeSymbol = _factory.SpecialType(SpecialType.System_Boolean)
                left = CreateBuiltInConversion(resultType, systemBool, left, node.Checked, False, ConversionSemantics.[Default])
                right = CreateBuiltInConversion(resultType, systemBool, right, node.Checked, False, ConversionSemantics.[Default])
            End If

            Dim isChecked As Boolean = node.Checked AndAlso IsIntegralType(resultUnderlyingType)
            Dim opFactory As WellKnownMember

            Select Case opKind
                Case BinaryOperatorKind.AndAlso
                    opFactory = WellKnownMember.System_Linq_Expressions_Expression__AndAlso
                Case BinaryOperatorKind.OrElse
                    opFactory = WellKnownMember.System_Linq_Expressions_Expression__OrElse
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select

            Dim result As BoundExpression = ConvertRuntimeHelperToExpressionTree(opFactory, left, right)

            If resultUnderlyingType.IsObjectType Then
                result = Convert(result, resultType, isChecked)
            End If

            Return result
        End Function

#End Region

#Region "And, Or, Xor, ^, *, +, -, /, \, Mod, <<, >>"

        Private Function ConvertBinaryOperator(node As BoundBinaryOperator) As BoundExpression
            Dim resultType As TypeSymbol = node.Type

            Dim resultNotNullableType As TypeSymbol = resultType.GetNullableUnderlyingTypeOrSelf
            Dim resultUnderlyingType As TypeSymbol = resultNotNullableType.GetEnumUnderlyingTypeOrSelf
            Dim resultUnderlyingSpecialType As SpecialType = resultUnderlyingType.SpecialType

            Dim opKind = node.OperatorKind And BinaryOperatorKind.OpMask
            Debug.Assert(opKind = BinaryOperatorKind.And OrElse opKind = BinaryOperatorKind.Or OrElse opKind = BinaryOperatorKind.Xor OrElse
                         opKind = BinaryOperatorKind.Power OrElse opKind = BinaryOperatorKind.Multiply OrElse
                         opKind = BinaryOperatorKind.Add OrElse opKind = BinaryOperatorKind.Subtract OrElse
                         opKind = BinaryOperatorKind.Divide OrElse opKind = BinaryOperatorKind.IntegerDivide OrElse
                         opKind = BinaryOperatorKind.Modulo OrElse
                         opKind = BinaryOperatorKind.LeftShift OrElse opKind = BinaryOperatorKind.RightShift)
            Debug.Assert((node.OperatorKind And BinaryOperatorKind.UserDefined) = 0)

            ' Do we need to use special helpers?
            Dim helper As MethodSymbol = Nothing
            If resultUnderlyingSpecialType = SpecialType.System_Object Then
                helper = GetHelperForObjectBinaryOperation(opKind)
                If helper Is Nothing Then ' Don't know how to do 'BinaryOperatorKind.Power' without the method
                    Return _factory.BadExpression(Visit(node.Left), Visit(node.Right))
                End If
            ElseIf resultUnderlyingSpecialType = SpecialType.System_Decimal Then
                helper = GetHelperForDecimalBinaryOperation(opKind)
                If helper Is Nothing Then ' Don't know how to do 'BinaryOperatorKind.Power' without the method
                    Return _factory.BadExpression(Visit(node.Left), Visit(node.Right))
                End If
            ElseIf opKind = BinaryOperatorKind.Power Then
                helper = Me._factory.WellKnownMember(Of MethodSymbol)(WellKnownMember.System_Math__PowDoubleDouble)
                If helper Is Nothing Then ' Don't know how to do 'BinaryOperatorKind.Power' without the method
                    Return _factory.BadExpression(Visit(node.Left), Visit(node.Right))
                End If
            End If

            Debug.Assert(opKind <> BinaryOperatorKind.Power OrElse helper IsNot Nothing)

            Dim isChecked As Boolean = node.Checked AndAlso IsIntegralType(resultUnderlyingType)

            Dim left As BoundExpression = Visit(node.Left)
            Dim right As BoundExpression

            If helper IsNot Nothing Then
                right = Visit(node.Right)
                Return ConvertRuntimeHelperToExpressionTree(GetNonComparisonBinaryOperatorFactoryWithMethodInfo(opKind, isChecked),
                                                           left, right, _factory.MethodInfo(helper, _factory.WellKnownType(WellKnownType.System_Reflection_MethodInfo)))
            End If

            ' No special helper
            Dim resultTypeIsNullable As Boolean = resultType.IsNullableType
            Dim needToCastBackToByteOrSByte As Boolean = resultUnderlyingSpecialType = SpecialType.System_Byte OrElse
                                                         resultUnderlyingSpecialType = SpecialType.System_SByte

            left = GenerateCastsForBinaryAndUnaryOperator(left,
                                                          resultTypeIsNullable,
                                                          resultNotNullableType,
                                                          isChecked AndAlso IsIntegralType(resultUnderlyingType),
                                                          needToCastBackToByteOrSByte)

            If opKind = BinaryOperatorKind.LeftShift OrElse opKind = BinaryOperatorKind.RightShift Then
                ' Add mask for right operand of a shift operator.
                right = MaskShiftCountOperand(node, resultType, isChecked)
                isChecked = False  ' final conversion shouldn't be checked for shift operands.

            Else
                right = Visit(node.Right)
                right = GenerateCastsForBinaryAndUnaryOperator(right,
                                                               resultTypeIsNullable,
                                                               resultNotNullableType,
                                                               isChecked AndAlso IsIntegralType(resultUnderlyingType),
                                                               needToCastBackToByteOrSByte)
            End If

            Dim result As BoundExpression = ConvertRuntimeHelperToExpressionTree(GetNonComparisonBinaryOperatorFactoryWithoutMethodInfo(opKind, isChecked), left, right)

            If needToCastBackToByteOrSByte Then
                Debug.Assert(resultUnderlyingSpecialType = SpecialType.System_Byte OrElse resultUnderlyingSpecialType = SpecialType.System_SByte)
                result = Convert(result, If(resultTypeIsNullable, Me._factory.NullableOf(resultUnderlyingType), resultUnderlyingType), isChecked)
            End If

            If resultNotNullableType.IsEnumType Then
                result = Convert(result, resultType, False)
            End If

            Return result
        End Function

        Private Shared Function GetNonComparisonBinaryOperatorFactoryWithoutMethodInfo(opKind As BinaryOperatorKind, isChecked As Boolean) As WellKnownMember
            Select Case opKind And BinaryOperatorKind.OpMask
                Case BinaryOperatorKind.Add
                    Return If(isChecked,
                              WellKnownMember.System_Linq_Expressions_Expression__AddChecked,
                              WellKnownMember.System_Linq_Expressions_Expression__Add)

                Case BinaryOperatorKind.Subtract
                    Return If(isChecked,
                              WellKnownMember.System_Linq_Expressions_Expression__SubtractChecked,
                              WellKnownMember.System_Linq_Expressions_Expression__Subtract)

                Case BinaryOperatorKind.Multiply
                    Return If(isChecked,
                              WellKnownMember.System_Linq_Expressions_Expression__MultiplyChecked,
                              WellKnownMember.System_Linq_Expressions_Expression__Multiply)

                Case BinaryOperatorKind.IntegerDivide,
                     BinaryOperatorKind.Divide
                    Return WellKnownMember.System_Linq_Expressions_Expression__Divide

                Case BinaryOperatorKind.Modulo
                    Return WellKnownMember.System_Linq_Expressions_Expression__Modulo

                Case BinaryOperatorKind.And
                    Return WellKnownMember.System_Linq_Expressions_Expression__And

                Case BinaryOperatorKind.Or
                    Return WellKnownMember.System_Linq_Expressions_Expression__Or

                Case BinaryOperatorKind.Xor
                    Return WellKnownMember.System_Linq_Expressions_Expression__ExclusiveOr

                Case BinaryOperatorKind.LeftShift
                    Return WellKnownMember.System_Linq_Expressions_Expression__LeftShift

                Case BinaryOperatorKind.RightShift
                    Return WellKnownMember.System_Linq_Expressions_Expression__RightShift

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select
        End Function

        Private Function GenerateCastsForBinaryAndUnaryOperator(loweredOperand As BoundExpression,
                                                                isNullable As Boolean,
                                                                notNullableType As TypeSymbol,
                                                                checked As Boolean,
                                                                needToCastBackToByteOrSByte As Boolean) As BoundExpression

            ' Convert enums to their underlying types
            If notNullableType.IsEnumType Then
                Dim underlyingType As TypeSymbol = notNullableType.GetEnumUnderlyingTypeOrSelf
                loweredOperand = Convert(loweredOperand, If(isNullable, Me._factory.NullableOf(underlyingType), underlyingType), False)
            End If

            ' Byte and SByte promote operators to Int32, then demote after
            If needToCastBackToByteOrSByte Then
                loweredOperand = Convert(loweredOperand, If(isNullable, Me._factory.NullableOf(Me.Int32Type), Me.Int32Type), checked)
            End If

            Return loweredOperand
        End Function

        Private Function MaskShiftCountOperand(node As BoundBinaryOperator, resultType As TypeSymbol, isChecked As Boolean) As BoundExpression
            Dim result As BoundExpression = Nothing

            ' NOTE: In case we have lifted binary operator, the original non-nullable right operand 
            '       might be already converted to correspondent nullable type; we want to disregard 
            '       such conversion before applying the mask and re-apply it after that
            Dim applyConversionToNullable As Boolean = False
            Dim originalRight As BoundExpression = node.Right

            Debug.Assert(Not resultType.GetNullableUnderlyingTypeOrSelf().IsEnumType)
            Dim shiftMask As Integer = CodeGen.CodeGenerator.GetShiftSizeMask(resultType.GetNullableUnderlyingTypeOrSelf())

            Dim shiftedType As TypeSymbol = resultType

            If resultType.IsNullableType AndAlso originalRight.Kind = BoundKind.Conversion Then
                Dim conversion = DirectCast(originalRight, BoundConversion)
                Dim operand As BoundExpression = conversion.Operand
                Dim operandType As TypeSymbol = operand.Type

                If ((conversion.ConversionKind And ConversionKind.Nullable) <> 0) AndAlso Not conversion.ExplicitCastInCode AndAlso Not operandType.IsNullableType Then
                    Debug.Assert(conversion.Type.IsNullableType AndAlso conversion.Type.GetNullableUnderlyingType.SpecialType = SpecialType.System_Int32)

                    ' type of the right operand before conversion
                    shiftedType = operandType.GetEnumUnderlyingTypeOrSelf()

                    ' visit and convert operand
                    result = Visit(operand)
                    If Not TypeSymbol.Equals(operandType, Me.Int32Type, TypeCompareKind.ConsiderEverything) Then
                        result = CreateBuiltInConversion(operandType, Me.Int32Type, result, isChecked, False, ConversionSemantics.[Default])
                    End If

                    applyConversionToNullable = True
                End If
            End If

            If Not applyConversionToNullable Then
                result = Visit(originalRight)
            End If

            ' Add mask for right operand of a shift operator.
            result = MaskShiftCountOperand(result, shiftedType, shiftMask, result.ConstantValueOpt, isChecked)

            If applyConversionToNullable Then
                result = Convert(result, Me._factory.NullableOf(Me.Int32Type), isChecked)
            End If

            Return result
        End Function

        ''' <summary>
        ''' The shift count for a left-shift or right-shift operator needs to be masked according to the type 
        ''' of the left hand side, unless the shift count is an in-range constant. This is similar to what is 
        ''' done in code gen.
        ''' </summary>
        Private Function MaskShiftCountOperand(loweredOperand As BoundExpression, shiftedType As TypeSymbol, shiftMask As Integer, shiftConst As ConstantValue, isChecked As Boolean) As BoundExpression
            If shiftConst Is Nothing OrElse shiftConst.UInt32Value > shiftMask Then
                Dim constantOperand As BoundExpression =
                    _factory.Convert(_expressionType,
                                     ConvertRuntimeHelperToExpressionTree(
                                        WellKnownMember.System_Linq_Expressions_Expression__Constant,
                                        Me._factory.Convert(Me.ObjectType, Me._factory.Literal(shiftMask)),
                                        Me._factory.Typeof(Me.Int32Type, _factory.WellKnownType(WellKnownType.System_Type))))

                Dim isNullable As Boolean = shiftedType.IsNullableType
                Dim isInt32 As Boolean = shiftedType.GetNullableUnderlyingTypeOrSelf.SpecialType = SpecialType.System_Int32

                Dim int32Nullable As TypeSymbol = If(isNullable, Me._factory.NullableOf(Me.Int32Type), Nothing)

                If isNullable Then
                    constantOperand = Convert(constantOperand, int32Nullable, isChecked)
                End If

                loweredOperand = _factory.Convert(_expressionType,
                                                  ConvertRuntimeHelperToExpressionTree(
                                                      WellKnownMember.System_Linq_Expressions_Expression__And,
                                                      loweredOperand,
                                                      constantOperand))
            End If

            Return loweredOperand
        End Function

#End Region

#Region "Utility"

        Private Function GetHelperForDecimalBinaryOperation(opKind As BinaryOperatorKind) As MethodSymbol
            opKind = opKind And BinaryOperatorKind.OpMask

            Dim specialHelper As SpecialMember
            Select Case opKind
                Case BinaryOperatorKind.Add
                    specialHelper = SpecialMember.System_Decimal__AddDecimalDecimal
                Case BinaryOperatorKind.Subtract
                    specialHelper = SpecialMember.System_Decimal__SubtractDecimalDecimal
                Case BinaryOperatorKind.Multiply
                    specialHelper = SpecialMember.System_Decimal__MultiplyDecimalDecimal
                Case BinaryOperatorKind.Divide
                    specialHelper = SpecialMember.System_Decimal__DivideDecimalDecimal
                Case BinaryOperatorKind.Modulo
                    specialHelper = SpecialMember.System_Decimal__ModuloDecimalDecimal

                Case BinaryOperatorKind.Equals,
                     BinaryOperatorKind.Is
                    specialHelper = SpecialMember.System_Decimal__op_Equality
                Case BinaryOperatorKind.NotEquals,
                     BinaryOperatorKind.IsNot
                    specialHelper = SpecialMember.System_Decimal__op_Inequality
                Case BinaryOperatorKind.LessThan
                    specialHelper = SpecialMember.System_Decimal__op_LessThan
                Case BinaryOperatorKind.LessThanOrEqual
                    specialHelper = SpecialMember.System_Decimal__op_LessThanOrEqual
                Case BinaryOperatorKind.GreaterThan
                    specialHelper = SpecialMember.System_Decimal__op_GreaterThan
                Case BinaryOperatorKind.GreaterThanOrEqual
                    specialHelper = SpecialMember.System_Decimal__op_GreaterThanOrEqual
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select

            Return DirectCast(_factory.SpecialMember(specialHelper), MethodSymbol)
        End Function

        Private Function GetHelperForDateTimeBinaryOperation(opKind As BinaryOperatorKind) As MethodSymbol
            opKind = opKind And BinaryOperatorKind.OpMask

            Dim specialHelper As SpecialMember
            Select Case opKind
                Case BinaryOperatorKind.Equals,
                     BinaryOperatorKind.Is
                    specialHelper = SpecialMember.System_DateTime__op_Equality
                Case BinaryOperatorKind.NotEquals,
                     BinaryOperatorKind.IsNot
                    specialHelper = SpecialMember.System_DateTime__op_Inequality
                Case BinaryOperatorKind.LessThan
                    specialHelper = SpecialMember.System_DateTime__op_LessThan
                Case BinaryOperatorKind.LessThanOrEqual
                    specialHelper = SpecialMember.System_DateTime__op_LessThanOrEqual
                Case BinaryOperatorKind.GreaterThan
                    specialHelper = SpecialMember.System_DateTime__op_GreaterThan
                Case BinaryOperatorKind.GreaterThanOrEqual
                    specialHelper = SpecialMember.System_DateTime__op_GreaterThanOrEqual

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select

            Return DirectCast(_factory.SpecialMember(specialHelper), MethodSymbol)
        End Function

        Private Function GetHelperForObjectBinaryOperation(opKind As BinaryOperatorKind) As MethodSymbol
            opKind = opKind And BinaryOperatorKind.OpMask

            Dim wellKnownHelper As WellKnownMember
            Select Case opKind
                Case BinaryOperatorKind.Add
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__AddObjectObjectObject
                Case BinaryOperatorKind.Subtract
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__SubtractObjectObjectObject
                Case BinaryOperatorKind.Multiply
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__MultiplyObjectObjectObject
                Case BinaryOperatorKind.Divide
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__DivideObjectObjectObject
                Case BinaryOperatorKind.IntegerDivide
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__IntDivideObjectObjectObject
                Case BinaryOperatorKind.Modulo
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ModObjectObjectObject
                Case BinaryOperatorKind.Power
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ExponentObjectObjectObject
                Case BinaryOperatorKind.And
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__AndObjectObjectObject
                Case BinaryOperatorKind.Xor
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__XorObjectObjectObject
                Case BinaryOperatorKind.Or
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__OrObjectObjectObject
                Case BinaryOperatorKind.LeftShift
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__LeftShiftObjectObjectObject
                Case BinaryOperatorKind.RightShift
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__RightShiftObjectObjectObject
                Case BinaryOperatorKind.Concatenate
                    wellKnownHelper = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConcatenateObjectObjectObject

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select

            Return Me._factory.WellKnownMember(Of MethodSymbol)(wellKnownHelper)
        End Function

        Private Shared Function AdjustCallArgumentForLiftedOperator(oldArg As BoundExpression, parameterType As TypeSymbol) As BoundExpression
            Debug.Assert(oldArg.Type.IsNullableType)
            Debug.Assert(Not parameterType.IsNullableType)
            Debug.Assert(TypeSymbol.Equals(oldArg.Type.GetNullableUnderlyingTypeOrSelf(), parameterType, TypeCompareKind.ConsiderEverything))

            If oldArg.Kind = BoundKind.ObjectCreationExpression Then
                Dim objCreation = DirectCast(oldArg, BoundObjectCreationExpression)
                ' Nullable<T> has only one ctor with parameters and only that one sets hasValue = true
                If objCreation.Arguments.Length = 1 Then
                    Return objCreation.Arguments(0)
                End If
            End If

            ' Else just wrap in conversion
            Return New BoundConversion(oldArg.Syntax, oldArg, ConversionKind.NarrowingNullable, False, False, parameterType)
        End Function

        Private Function AdjustCallForLiftedOperator(opKind As BinaryOperatorKind, [call] As BoundCall, resultType As TypeSymbol) As BoundExpression
            Debug.Assert((opKind And BinaryOperatorKind.Lifted) <> 0)
            Debug.Assert((opKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.Like OrElse
                         (opKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.Concatenate)
            Return AdjustCallForLiftedOperator_DoNotCallDirectly([call], resultType)
        End Function

        Private Function AdjustCallForLiftedOperator(opKind As UnaryOperatorKind, [call] As BoundCall, resultType As TypeSymbol) As BoundExpression
            Debug.Assert((opKind And UnaryOperatorKind.Lifted) <> 0)
            Debug.Assert((opKind And UnaryOperatorKind.OpMask) = UnaryOperatorKind.IsTrue OrElse
                         (opKind And UnaryOperatorKind.OpMask) = UnaryOperatorKind.IsFalse)
            Return AdjustCallForLiftedOperator_DoNotCallDirectly([call], resultType)
        End Function

        Private Function AdjustCallForLiftedOperator_DoNotCallDirectly([call] As BoundCall, resultType As TypeSymbol) As BoundExpression
            ' NOTE: those operators which are not converted into a special factory methods, but rather 
            '       into a direct call we need to adjust the type of operands and the resulting type to 
            '       that of the method. This method is only to be called for particular operators!!!!

            Dim parameters As ImmutableArray(Of ParameterSymbol) = [call].Method.Parameters
            Debug.Assert(parameters.Length > 0)

            Dim oldArgs As ImmutableArray(Of BoundExpression) = [call].Arguments
            Debug.Assert(parameters.Length = oldArgs.Length)

            Dim newArgs(oldArgs.Length - 1) As BoundExpression
            For i = 0 To oldArgs.Length - 1
                newArgs(i) = AdjustCallArgumentForLiftedOperator(oldArgs(i), parameters(i).Type)
            Next

            Dim methodReturnType As TypeSymbol = [call].Method.ReturnType
            Debug.Assert(resultType.GetNullableUnderlyingTypeOrSelf().
                         IsSameTypeIgnoringAll(methodReturnType.GetNullableUnderlyingTypeOrSelf))

            [call] = [call].Update([call].Method,
                                   [call].MethodGroupOpt,
                                   [call].ReceiverOpt,
                                   newArgs.AsImmutableOrNull,
                                   [call].DefaultArguments,
                                   [call].ConstantValueOpt,
                                   isLValue:=[call].IsLValue,
                                   suppressObjectClone:=[call].SuppressObjectClone,
                                   type:=methodReturnType)

            If resultType.IsNullableType <> methodReturnType.IsNullableType Then
                Return Me._factory.Convert(resultType, [call])
            End If
            Return [call]
        End Function

#End Region

    End Class
End Namespace
