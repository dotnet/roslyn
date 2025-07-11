' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen

    Partial Friend Class CodeGenerator

        Private Sub EmitUnaryOperatorExpression(expression As BoundUnaryOperator, used As Boolean)
            Debug.Assert((expression.OperatorKind And Not UnaryOperatorKind.IntrinsicOpMask) = 0 AndAlso expression.OperatorKind <> 0)

            If Not used AndAlso Not OperatorHasSideEffects(expression) Then
                EmitExpression(expression.Operand, used:=False)
                Return
            End If

            Select Case expression.OperatorKind
                Case UnaryOperatorKind.Minus

                    ' If overflow checking is on, we must subtract from zero because Neg doesn't
                    ' check for overflow.
                    Dim targetPrimitiveType = expression.Type.PrimitiveTypeCode
                    Dim useCheckedSubtraction As Boolean = (expression.Checked AndAlso
                                                     (targetPrimitiveType = Cci.PrimitiveTypeCode.Int32 OrElse
                                                      targetPrimitiveType = Cci.PrimitiveTypeCode.Int64))

                    If useCheckedSubtraction Then
                        ' Generate the zero const first.
                        _builder.EmitOpCode(ILOpCode.Ldc_i4_0)

                        If targetPrimitiveType = Cci.PrimitiveTypeCode.Int64 Then
                            _builder.EmitOpCode(ILOpCode.Conv_i8)
                        End If
                    End If

                    EmitExpression(expression.Operand, used:=True)

                    If useCheckedSubtraction Then
                        _builder.EmitOpCode(ILOpCode.Sub_ovf)
                    Else
                        _builder.EmitOpCode(ILOpCode.Neg)
                    End If

                    ' The result of the math operation has either 4 or 8 byte width.
                    ' For 1 and 2 byte widths, convert the value back to the original type.
                    DowncastResultOfArithmeticOperation(targetPrimitiveType, expression.Checked)

                Case UnaryOperatorKind.Not

                    If expression.Type.IsBooleanType() Then
                        ' ISSUE: Will this emit 0 for false and 1 for true or can it leave non-zero on the stack without mapping it to 1?
                        '        Dev10 code gen made sure there is either 0 or 1 on the stack after this operation.
                        EmitCondExpr(expression.Operand, sense:=False)
                    Else
                        EmitExpression(expression.Operand, used:=True)
                        _builder.EmitOpCode(ILOpCode.Not)

                        ' Since the CLR will generate a 4-byte result from the Not operation, we
                        ' need to convert back to ui1 or ui2 because they are unsigned
                        ' CONSIDER cambecc (8-2-2000): no need to generate a Convert for each of n consecutive
                        '                              Not operations
                        Dim targetPrimitiveType = expression.Type.PrimitiveTypeCode
                        If targetPrimitiveType = Cci.PrimitiveTypeCode.UInt8 OrElse
                           targetPrimitiveType = Cci.PrimitiveTypeCode.UInt16 Then

                            _builder.EmitNumericConversion(Cci.PrimitiveTypeCode.UInt32,
                                                            targetPrimitiveType, False)
                        End If
                    End If

                Case UnaryOperatorKind.Plus
                    EmitExpression(expression.Operand, used:=True)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(expression.OperatorKind)
            End Select

            EmitPopIfUnused(used)
        End Sub

        Private Shared Function OperatorHasSideEffects(expression As BoundUnaryOperator) As Boolean
            If expression.Checked AndAlso
               expression.OperatorKind = UnaryOperatorKind.Minus AndAlso
               expression.Type.IsIntegralType() Then
                Return True
            End If

            Return False
        End Function

        Private Sub EmitBinaryOperatorExpression(expression As BoundBinaryOperator, used As Boolean)

            Dim operationKind = expression.OperatorKind And BinaryOperatorKind.OpMask
            Dim shortCircuit As Boolean = operationKind = BinaryOperatorKind.AndAlso OrElse operationKind = BinaryOperatorKind.OrElse

            If Not used AndAlso Not shortCircuit AndAlso Not OperatorHasSideEffects(expression) Then
                EmitExpression(expression.Left, False)
                EmitExpression(expression.Right, False)
                Return
            End If

            If IsCondOperator(operationKind) Then
                EmitBinaryCondOperator(expression, True)
            Else
                EmitBinaryOperator(expression)
            End If

            EmitPopIfUnused(used)
        End Sub

        Private Function IsCondOperator(operationKind As BinaryOperatorKind) As Boolean
            Select Case (operationKind And BinaryOperatorKind.OpMask)
                Case BinaryOperatorKind.OrElse,
                     BinaryOperatorKind.AndAlso,
                     BinaryOperatorKind.Equals,
                     BinaryOperatorKind.NotEquals,
                     BinaryOperatorKind.LessThanOrEqual,
                     BinaryOperatorKind.GreaterThanOrEqual,
                     BinaryOperatorKind.LessThan,
                     BinaryOperatorKind.GreaterThan,
                     BinaryOperatorKind.Is,
                     BinaryOperatorKind.IsNot

                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Private Function IsComparisonOperator(operationKind As BinaryOperatorKind) As Boolean
            Select Case operationKind And BinaryOperatorKind.OpMask
                Case BinaryOperatorKind.Equals,
                     BinaryOperatorKind.NotEquals,
                     BinaryOperatorKind.LessThanOrEqual,
                     BinaryOperatorKind.GreaterThanOrEqual,
                     BinaryOperatorKind.LessThan,
                     BinaryOperatorKind.GreaterThan,
                     BinaryOperatorKind.Is,
                     BinaryOperatorKind.IsNot
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Private Sub EmitBinaryOperator(expression As BoundBinaryOperator)
            ' Do not blow the stack due to a deep recursion on the left. 

            Dim child As BoundExpression = expression.Left

            If child.Kind <> BoundKind.BinaryOperator OrElse child.ConstantValueOpt IsNot Nothing Then
                EmitBinaryOperatorSimple(expression)
                Return
            End If

            Dim binary As BoundBinaryOperator = DirectCast(child, BoundBinaryOperator)

            If IsCondOperator(binary.OperatorKind) Then
                EmitBinaryOperatorSimple(expression)
                Return
            End If

            Dim stack = ArrayBuilder(Of BoundBinaryOperator).GetInstance()
            stack.Push(expression)

            Do
                stack.Push(binary)
                child = binary.Left

                If child.Kind <> BoundKind.BinaryOperator OrElse child.ConstantValueOpt IsNot Nothing Then
                    Exit Do
                End If

                binary = DirectCast(child, BoundBinaryOperator)

                If IsCondOperator(binary.OperatorKind) Then
                    Exit Do
                End If
            Loop

            EmitExpression(child, True)

            Do
                binary = stack.Pop()

                EmitExpression(binary.Right, True)

                Select Case (binary.OperatorKind And BinaryOperatorKind.OpMask)
                    Case BinaryOperatorKind.And
                        _builder.EmitOpCode(ILOpCode.And)

                    Case BinaryOperatorKind.Xor
                        _builder.EmitOpCode(ILOpCode.Xor)

                    Case BinaryOperatorKind.Or
                        _builder.EmitOpCode(ILOpCode.Or)

                    Case Else
                        EmitBinaryArithOperatorInstructionAndDowncast(binary)
                End Select
            Loop While binary IsNot expression

            Debug.Assert(stack.Count = 0)
            stack.Free()
        End Sub

        Private Sub EmitBinaryOperatorSimple(expression As BoundBinaryOperator)

            Select Case (expression.OperatorKind And BinaryOperatorKind.OpMask)
                Case BinaryOperatorKind.And
                    EmitExpression(expression.Left, True)
                    EmitExpression(expression.Right, True)
                    _builder.EmitOpCode(ILOpCode.And)

                Case BinaryOperatorKind.Xor
                    EmitExpression(expression.Left, True)
                    EmitExpression(expression.Right, True)
                    _builder.EmitOpCode(ILOpCode.Xor)

                Case BinaryOperatorKind.Or
                    EmitExpression(expression.Left, True)
                    EmitExpression(expression.Right, True)
                    _builder.EmitOpCode(ILOpCode.Or)

                Case Else
                    EmitBinaryArithOperator(expression)
            End Select
        End Sub

        Private Function OperatorHasSideEffects(expression As BoundBinaryOperator) As Boolean
            Dim type = expression.OperatorKind And BinaryOperatorKind.OpMask

            Select Case type
                Case BinaryOperatorKind.Divide,
                     BinaryOperatorKind.Modulo,
                     BinaryOperatorKind.IntegerDivide
                    Return True

                Case BinaryOperatorKind.Multiply,
                     BinaryOperatorKind.Add,
                     BinaryOperatorKind.Subtract
                    Return expression.Checked AndAlso expression.Type.IsIntegralType()

                Case Else
                    Return False
            End Select
        End Function

        Private Sub EmitBinaryArithOperator(expression As BoundBinaryOperator)

            EmitExpression(expression.Left, True)
            EmitExpression(expression.Right, True)

            EmitBinaryArithOperatorInstructionAndDowncast(expression)
        End Sub

        Private Sub EmitBinaryArithOperatorInstructionAndDowncast(expression As BoundBinaryOperator)
            Dim targetPrimitiveType = expression.Type.PrimitiveTypeCode
            Dim opKind = expression.OperatorKind And BinaryOperatorKind.OpMask

            Select Case opKind
                Case BinaryOperatorKind.Multiply

                    If expression.Checked AndAlso
                        (targetPrimitiveType = Cci.PrimitiveTypeCode.Int32 OrElse targetPrimitiveType = Cci.PrimitiveTypeCode.Int64) Then
                        _builder.EmitOpCode(ILOpCode.Mul_ovf)

                    ElseIf expression.Checked AndAlso
                        (targetPrimitiveType = Cci.PrimitiveTypeCode.UInt32 OrElse targetPrimitiveType = Cci.PrimitiveTypeCode.UInt64) Then
                        _builder.EmitOpCode(ILOpCode.Mul_ovf_un)
                    Else
                        _builder.EmitOpCode(ILOpCode.Mul)
                    End If

                Case BinaryOperatorKind.Modulo
                    If targetPrimitiveType.IsUnsigned() Then
                        _builder.EmitOpCode(ILOpCode.Rem_un)
                    Else
                        _builder.EmitOpCode(ILOpCode.[Rem])
                    End If

                Case BinaryOperatorKind.Add
                    If expression.Checked AndAlso
                        (targetPrimitiveType = Cci.PrimitiveTypeCode.Int32 OrElse targetPrimitiveType = Cci.PrimitiveTypeCode.Int64) Then
                        _builder.EmitOpCode(ILOpCode.Add_ovf)

                    ElseIf expression.Checked AndAlso
                        (targetPrimitiveType = Cci.PrimitiveTypeCode.UInt32 OrElse targetPrimitiveType = Cci.PrimitiveTypeCode.UInt64) Then
                        _builder.EmitOpCode(ILOpCode.Add_ovf_un)
                    Else
                        _builder.EmitOpCode(ILOpCode.Add)
                    End If

                Case BinaryOperatorKind.Subtract
                    If expression.Checked AndAlso
                        (targetPrimitiveType = Cci.PrimitiveTypeCode.Int32 OrElse targetPrimitiveType = Cci.PrimitiveTypeCode.Int64) Then
                        _builder.EmitOpCode(ILOpCode.Sub_ovf)

                    ElseIf expression.Checked AndAlso
                        (targetPrimitiveType = Cci.PrimitiveTypeCode.UInt32 OrElse targetPrimitiveType = Cci.PrimitiveTypeCode.UInt64) Then
                        _builder.EmitOpCode(ILOpCode.Sub_ovf_un)
                    Else
                        _builder.EmitOpCode(ILOpCode.Sub)
                    End If

                Case BinaryOperatorKind.Divide,
                     BinaryOperatorKind.IntegerDivide

                    If targetPrimitiveType.IsUnsigned() Then
                        _builder.EmitOpCode(ILOpCode.Div_un)
                    Else
                        _builder.EmitOpCode(ILOpCode.Div)
                    End If

                Case BinaryOperatorKind.LeftShift

                    ' And the right operand with mask corresponding the left operand type
                    Debug.Assert(expression.Right.Type.PrimitiveTypeCode = Cci.PrimitiveTypeCode.Int32)
                    'mask RHS if not a constant or too large
                    Dim shiftMax = GetShiftSizeMask(expression.Left.Type)
                    Dim shiftConst = expression.Right.ConstantValueOpt
                    If shiftConst Is Nothing OrElse shiftConst.UInt32Value > shiftMax Then
                        _builder.EmitConstantValue(ConstantValue.Create(shiftMax), expression.Right.Syntax)
                        _builder.EmitOpCode(ILOpCode.And)
                    End If

                    _builder.EmitOpCode(ILOpCode.Shl)

                Case BinaryOperatorKind.RightShift

                    ' And the right operand with mask corresponding the left operand type
                    Debug.Assert(expression.Right.Type.PrimitiveTypeCode = Cci.PrimitiveTypeCode.Int32)

                    'mask RHS if not a constant or too large
                    Dim shiftMax = GetShiftSizeMask(expression.Left.Type)
                    Dim shiftConst = expression.Right.ConstantValueOpt
                    If shiftConst Is Nothing OrElse shiftConst.UInt32Value > shiftMax Then
                        _builder.EmitConstantValue(ConstantValue.Create(shiftMax), expression.Right.Syntax)
                        _builder.EmitOpCode(ILOpCode.And)
                    End If

                    If targetPrimitiveType.IsUnsigned() Then
                        _builder.EmitOpCode(ILOpCode.Shr_un)
                    Else
                        _builder.EmitOpCode(ILOpCode.Shr)
                    End If

                Case Else
                    ' BinaryOperatorKind.Power, BinaryOperatorKind.Like and BinaryOperatorKind.Concatenate should go here.
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select

            ' The result of the math operation has either 4 or 8 byte width.
            ' For 1 and 2 byte widths, convert the value back to the original type.
            DowncastResultOfArithmeticOperation(targetPrimitiveType, expression.Checked AndAlso
                                                    opKind <> BinaryOperatorKind.LeftShift AndAlso
                                                    opKind <> BinaryOperatorKind.RightShift)

        End Sub

        Private Sub DowncastResultOfArithmeticOperation(
            targetPrimitiveType As Cci.PrimitiveTypeCode,
            isChecked As Boolean
        )
            ' The result of the math operation has either 4 or 8 byte width.
            ' For 1 and 2 byte widths, convert the value back to the original type.
            If targetPrimitiveType = Cci.PrimitiveTypeCode.Int8 OrElse
               targetPrimitiveType = Cci.PrimitiveTypeCode.UInt8 OrElse
               targetPrimitiveType = Cci.PrimitiveTypeCode.Int16 OrElse
               targetPrimitiveType = Cci.PrimitiveTypeCode.UInt16 Then

                _builder.EmitNumericConversion(If(targetPrimitiveType.IsUnsigned(), Cci.PrimitiveTypeCode.UInt32, Cci.PrimitiveTypeCode.Int32),
                                               targetPrimitiveType,
                                               isChecked)
            End If

        End Sub

        Public Shared Function GetShiftSizeMask(leftOperandType As TypeSymbol) As Integer
            Return leftOperandType.GetEnumUnderlyingTypeOrSelf.SpecialType.GetShiftSizeMask()
        End Function

        Private Sub EmitShortCircuitingOperator(condition As BoundBinaryOperator, sense As Boolean, stopSense As Boolean, stopValue As Boolean)
            ' we generate:
            '
            ' gotoif (a == stopSense) fallThrough
            ' b == sense
            ' goto labEnd
            ' fallThrough:
            ' stopValue
            ' labEnd:
            ' AND OR
            ' +- ------ -----
            ' stopSense | !sense sense
            ' stopValue | 0 1
            Dim fallThrough As Object = Nothing

            EmitCondBranch(condition.Left, fallThrough, stopSense)
            EmitCondExpr(condition.Right, sense)

            ' if fall-through was not initialized, no-one is going to take that branch
            ' and we are done with Right on the stack
            If fallThrough Is Nothing Then
                Return
            End If

            Dim labEnd = New Object
            _builder.EmitBranch(ILOpCode.Br, labEnd)

            ' if we get to FallThrough we should not have Right on the stack. Adjust for that.
            _builder.AdjustStack(-1)

            _builder.MarkLabel(fallThrough)
            _builder.EmitBoolConstant(stopValue)
            _builder.MarkLabel(labEnd)
        End Sub

        'NOTE: odd positions assume inverted sense
        Private Shared ReadOnly s_compOpCodes As ILOpCode() = New ILOpCode() {ILOpCode.Clt, ILOpCode.Cgt, ILOpCode.Cgt, ILOpCode.Clt, ILOpCode.Clt_un, ILOpCode.Cgt_un, ILOpCode.Cgt_un, ILOpCode.Clt_un, ILOpCode.Clt, ILOpCode.Cgt_un, ILOpCode.Cgt, ILOpCode.Clt_un}

        'NOTE: The result of this should be a boolean on the stack.
        Private Sub EmitBinaryCondOperator(binOp As BoundBinaryOperator, sense As Boolean)
            Dim andOrSense As Boolean = sense
            Dim opIdx As Integer
            Dim opKind = (binOp.OperatorKind And BinaryOperatorKind.OpMask)
            Dim operandType = binOp.Left.Type

            Debug.Assert(operandType IsNot Nothing OrElse (binOp.Left.IsNothingLiteral() AndAlso (opKind = BinaryOperatorKind.Is OrElse opKind = BinaryOperatorKind.IsNot)))

            If operandType IsNot Nothing AndAlso operandType.IsBooleanType() Then
                ' Since VB True is -1 but is stored as 1 in IL, relational operations on Boolean must
                ' be reversed to yield the correct results. Note that = and <> do not need reversal.
                Select Case opKind
                    Case BinaryOperatorKind.LessThan
                        opKind = BinaryOperatorKind.GreaterThan
                    Case BinaryOperatorKind.LessThanOrEqual
                        opKind = BinaryOperatorKind.GreaterThanOrEqual
                    Case BinaryOperatorKind.GreaterThan
                        opKind = BinaryOperatorKind.LessThan
                    Case BinaryOperatorKind.GreaterThanOrEqual
                        opKind = BinaryOperatorKind.LessThanOrEqual
                End Select
            End If

            Select Case opKind
                Case BinaryOperatorKind.OrElse
                    andOrSense = Not andOrSense
                    GoTo BinaryOperatorKindLogicalAnd

                Case BinaryOperatorKind.AndAlso
BinaryOperatorKindLogicalAnd:
                    Debug.Assert(binOp.Left.Type.SpecialType = SpecialType.System_Boolean)
                    Debug.Assert(binOp.Right.Type.SpecialType = SpecialType.System_Boolean)

                    If Not andOrSense Then
                        EmitShortCircuitingOperator(binOp, sense, sense, True)
                    Else
                        EmitShortCircuitingOperator(binOp, sense, Not sense, False)
                    End If
                    Return

                Case BinaryOperatorKind.IsNot
                    ValidateReferenceEqualityOperands(binOp)
                    GoTo BinaryOperatorKindNotEqual

                Case BinaryOperatorKind.Is
                    ValidateReferenceEqualityOperands(binOp)
                    GoTo BinaryOperatorKindEqual

                Case BinaryOperatorKind.NotEquals
BinaryOperatorKindNotEqual:
                    sense = Not sense
                    GoTo BinaryOperatorKindEqual

                Case BinaryOperatorKind.Equals
BinaryOperatorKindEqual:
                    Dim constant = binOp.Left.ConstantValueOpt
                    Dim comparand = binOp.Right

                    If constant Is Nothing Then
                        constant = comparand.ConstantValueOpt
                        comparand = binOp.Left
                    End If

                    If constant IsNot Nothing Then
                        If constant.IsDefaultValue Then
                            If Not constant.IsFloating Then
                                If sense Then
                                    EmitIsNullOrZero(comparand, constant)
                                Else
                                    '  obj != null/0   for pointers and integral numerics is emitted as cgt.un
                                    EmitIsNotNullOrZero(comparand, constant)
                                End If
                                Return
                            End If
                        ElseIf constant.IsBoolean Then
                            ' treat  "x = True" ==> "x"
                            EmitExpression(comparand, True)
                            EmitIsSense(sense)
                            Return
                        End If
                    End If

                    EmitBinaryCondOperatorHelper(ILOpCode.Ceq, binOp.Left, binOp.Right, sense)
                    Return

                Case BinaryOperatorKind.Or
                    Debug.Assert(binOp.Left.Type.SpecialType = SpecialType.System_Boolean)
                    Debug.Assert(binOp.Right.Type.SpecialType = SpecialType.System_Boolean)

                    EmitBinaryCondOperatorHelper(ILOpCode.Or, binOp.Left, binOp.Right, sense)
                    Return

                Case BinaryOperatorKind.And
                    Debug.Assert(binOp.Left.Type.SpecialType = SpecialType.System_Boolean)
                    Debug.Assert(binOp.Right.Type.SpecialType = SpecialType.System_Boolean)

                    EmitBinaryCondOperatorHelper(ILOpCode.And, binOp.Left, binOp.Right, sense)
                    Return

                Case BinaryOperatorKind.Xor
                    Debug.Assert(binOp.Left.Type.SpecialType = SpecialType.System_Boolean)
                    Debug.Assert(binOp.Right.Type.SpecialType = SpecialType.System_Boolean)

                    ' Xor is equivalent to not equal.
                    If (sense) Then
                        EmitBinaryCondOperatorHelper(ILOpCode.Xor, binOp.Left, binOp.Right, True)
                    Else
                        EmitBinaryCondOperatorHelper(ILOpCode.Ceq, binOp.Left, binOp.Right, True)
                    End If

                    Return

                Case BinaryOperatorKind.LessThan
                    opIdx = 0

                Case BinaryOperatorKind.LessThanOrEqual
                    opIdx = 1
                    sense = Not sense

                Case BinaryOperatorKind.GreaterThan
                    opIdx = 2

                Case BinaryOperatorKind.GreaterThanOrEqual
                    opIdx = 3
                    sense = Not sense

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select

            If operandType IsNot Nothing Then
                If operandType.IsUnsignedIntegralType() Then
                    opIdx += 4
                Else
                    If operandType.IsFloatingType() Then
                        opIdx += 8
                    End If
                End If
            End If

            EmitBinaryCondOperatorHelper(s_compOpCodes(opIdx), binOp.Left, binOp.Right, sense)

            Return
        End Sub

        Private Sub EmitIsNotNullOrZero(comparand As BoundExpression, nullOrZero As ConstantValue)
            EmitExpression(comparand, True)

            Dim comparandType = comparand.Type
            If comparandType.IsReferenceType AndAlso Not IsVerifierReference(comparandType) Then
                EmitBox(comparandType, comparand.Syntax)
            End If

            _builder.EmitConstantValue(nullOrZero, comparand.Syntax)
            _builder.EmitOpCode(ILOpCode.Cgt_un)
        End Sub

        Private Sub EmitIsNullOrZero(comparand As BoundExpression, nullOrZero As ConstantValue)
            EmitExpression(comparand, True)

            Dim comparandType = comparand.Type
            If comparandType.IsReferenceType AndAlso Not IsVerifierReference(comparandType) Then
                EmitBox(comparandType, comparand.Syntax)
            End If

            _builder.EmitConstantValue(nullOrZero, comparand.Syntax)
            _builder.EmitOpCode(ILOpCode.Ceq)
        End Sub

        Private Sub EmitBinaryCondOperatorHelper(opCode As ILOpCode,
                                                 left As BoundExpression,
                                                 right As BoundExpression,
                                                 sense As Boolean)
            EmitExpression(left, True)
            EmitExpression(right, True)
            _builder.EmitOpCode(opCode)
            EmitIsSense(sense)
        End Sub

        ' generate a conditional (ie, boolean) expression...
        ' this will leave a value on the stack which conforms to sense, ie:(condition == sense)
        Private Function EmitCondExpr(condition As BoundExpression, sense As Boolean) As ConstResKind
            RemoveNegation(condition, sense)

            Debug.Assert(condition.Type.SpecialType = SpecialType.System_Boolean)

            If _ilEmitStyle = ILEmitStyle.Release AndAlso condition.IsConstant Then
                Dim constValue = condition.ConstantValueOpt
                Debug.Assert(constValue.IsBoolean)
                Dim constant = constValue.BooleanValue

                _builder.EmitBoolConstant(constant = sense)
                Return (If(constant = sense, ConstResKind.ConstTrue, ConstResKind.ConstFalse))
            End If

            If condition.Kind = BoundKind.BinaryOperator Then
                Dim binOp = DirectCast(condition, BoundBinaryOperator)
                EmitBinaryCondOperator(binOp, sense)
                Return ConstResKind.NotAConst
            End If

            EmitExpression(condition, True)
            EmitIsSense(sense)

            Return ConstResKind.NotAConst
        End Function

        ''' <summary>
        ''' Emits boolean expression without branching if possible (i.e., no logical operators, only comparisons).
        ''' Leaves a boolean (int32, 0 or 1) value on the stack which conforms to sense, i.e., <c>condition = sense</c>.
        ''' </summary>
        Private Function TryEmitComparison(condition As BoundExpression, sense As Boolean) As Boolean
            RemoveNegation(condition, sense)

            Debug.Assert(condition.Type.SpecialType = SpecialType.System_Boolean)

            If condition.IsConstant Then
                Dim constValue = condition.ConstantValueOpt
                Debug.Assert(constValue.IsBoolean)
                Dim constant = constValue.BooleanValue

                _builder.EmitBoolConstant(constant = sense)
                Return True
            End If

            If condition.Kind = BoundKind.BinaryOperator Then
                Dim binOp = DirectCast(condition, BoundBinaryOperator)
                ' Intentionally don't optimize logical operators, they need branches to short-circuit.
                If IsComparisonOperator(binOp.OperatorKind) Then
                    EmitBinaryCondOperator(binOp, sense)
                    Return True
                End If
            ElseIf condition.Kind = BoundKind.TypeOf Then
                Dim typeOfExpression = DirectCast(condition, BoundTypeOf)

                EmitTypeOfExpression(typeOfExpression, used:=True, optimize:=True)

                If typeOfExpression.IsTypeOfIsNotExpression Then
                    sense = Not sense
                End If

                ' Convert to 1 or 0.
                _builder.EmitOpCode(ILOpCode.Ldnull)
                _builder.EmitOpCode(If(sense, ILOpCode.Cgt_un, ILOpCode.Ceq))
                Return True
            Else
                EmitExpression(condition, used:=True)

                ' Convert to 1 or 0 (although `condition` is of type `Boolean`, it can contain any integer).
                _builder.EmitOpCode(ILOpCode.Ldc_i4_0)
                _builder.EmitOpCode(If(sense, ILOpCode.Cgt_un, ILOpCode.Ceq))
                Return True
            End If

            Return False
        End Function

        Private Sub RemoveNegation(ByRef condition As BoundExpression, ByRef sense As Boolean)
            While condition.Kind = BoundKind.UnaryOperator
                Dim unOp = DirectCast(condition, BoundUnaryOperator)
                Debug.Assert(unOp.OperatorKind = UnaryOperatorKind.Not AndAlso unOp.Type.IsBooleanType())
                condition = unOp.Operand
                sense = Not sense
            End While
        End Sub

        ' emits IsTrue/IsFalse according to the sense
        ' IsTrue actually does nothing
        Private Sub EmitIsSense(sense As Boolean)
            If Not sense Then
                _builder.EmitOpCode(ILOpCode.Ldc_i4_0)
                _builder.EmitOpCode(ILOpCode.Ceq)
            End If
        End Sub

    End Class

End Namespace

