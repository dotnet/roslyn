// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGen
{
    internal partial class CodeGenerator
    {
        private void EmitUnaryOperatorExpression(BoundUnaryOperator expression, bool used)
        {
            var operatorKind = expression.OperatorKind;

            if (operatorKind.IsChecked())
            {
                EmitUnaryCheckedOperatorExpression(expression, used);
                return;
            }

            if (!used)
            {
                EmitExpression(expression.Operand, used: false);
                return;
            }

            if (operatorKind == UnaryOperatorKind.BoolLogicalNegation)
            {
                EmitCondExpr(expression.Operand, sense: false);
                return;
            }

            EmitExpression(expression.Operand, used: true);
            switch (operatorKind.Operator())
            {
                case UnaryOperatorKind.UnaryMinus:
                    _builder.EmitOpCode(ILOpCode.Neg);
                    break;

                case UnaryOperatorKind.BitwiseComplement:
                    _builder.EmitOpCode(ILOpCode.Not);
                    break;

                case UnaryOperatorKind.UnaryPlus:
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(operatorKind.Operator());
            }
        }

        private void EmitBinaryOperatorExpression(BoundBinaryOperator expression, bool used)
        {
            var operatorKind = expression.OperatorKind;

            if (operatorKind.EmitsAsCheckedInstruction())
            {
                EmitBinaryOperator(expression);
            }
            else
            {
                // if operator does not have side-effects itself and is not short-circuiting
                // we can simply emit side-effects from the first operand and then from the second one
                if (!used && !operatorKind.IsLogical() && !OperatorHasSideEffects(operatorKind))
                {
                    EmitExpression(expression.Left, false);
                    EmitExpression(expression.Right, false);
                    return;
                }

                if (IsConditional(operatorKind))
                {
                    EmitBinaryCondOperator(expression, true);
                }
                else
                {
                    EmitBinaryOperator(expression);
                }
            }

            EmitPopIfUnused(used);
        }

        private void EmitBinaryOperator(BoundBinaryOperator expression)
        {
            BoundExpression child = expression.Left;

            if (child.Kind != BoundKind.BinaryOperator || child.ConstantValueOpt != null)
            {
                EmitBinaryOperatorSimple(expression);
                return;
            }

            BoundBinaryOperator binary = (BoundBinaryOperator)child;
            var operatorKind = binary.OperatorKind;

            if (!operatorKind.EmitsAsCheckedInstruction() && IsConditional(operatorKind))
            {
                EmitBinaryOperatorSimple(expression);
                return;
            }

            // Do not blow the stack due to a deep recursion on the left.
            var stack = ArrayBuilder<BoundBinaryOperator>.GetInstance();
            stack.Push(expression);

            while (true)
            {
                stack.Push(binary);
                child = binary.Left;

                if (child.Kind != BoundKind.BinaryOperator || child.ConstantValueOpt != null)
                {
                    break;
                }

                binary = (BoundBinaryOperator)child;
                operatorKind = binary.OperatorKind;

                if (!operatorKind.EmitsAsCheckedInstruction() && IsConditional(operatorKind))
                {
                    break;
                }
            }

            EmitExpression(child, true);

            do
            {
                binary = stack.Pop();

                EmitExpression(binary.Right, true);
                bool isChecked = binary.OperatorKind.EmitsAsCheckedInstruction();
                if (isChecked)
                {
                    EmitBinaryCheckedOperatorInstruction(binary);
                }
                else
                {
                    EmitBinaryOperatorInstruction(binary);
                }

                EmitConversionToEnumUnderlyingType(binary, @checked: isChecked);
            }
            while (stack.Count > 0);

            Debug.Assert((object)binary == expression);
            stack.Free();
        }

        private void EmitBinaryOperatorSimple(BoundBinaryOperator expression)
        {
            EmitExpression(expression.Left, true);
            EmitExpression(expression.Right, true);

            bool isChecked = expression.OperatorKind.EmitsAsCheckedInstruction();
            if (isChecked)
            {
                EmitBinaryCheckedOperatorInstruction(expression);
            }
            else
            {
                EmitBinaryOperatorInstruction(expression);
            }

            EmitConversionToEnumUnderlyingType(expression, @checked: isChecked);
        }

        private void EmitBinaryOperatorInstruction(BoundBinaryOperator expression)
        {
            switch (expression.OperatorKind.Operator())
            {
                case BinaryOperatorKind.Multiplication:
                    _builder.EmitOpCode(ILOpCode.Mul);
                    break;

                case BinaryOperatorKind.Addition:
                    _builder.EmitOpCode(ILOpCode.Add);
                    break;

                case BinaryOperatorKind.Subtraction:
                    _builder.EmitOpCode(ILOpCode.Sub);
                    break;

                case BinaryOperatorKind.Division:
                    if (IsUnsignedBinaryOperator(expression))
                    {
                        _builder.EmitOpCode(ILOpCode.Div_un);
                    }
                    else
                    {
                        _builder.EmitOpCode(ILOpCode.Div);
                    }
                    break;

                case BinaryOperatorKind.Remainder:
                    if (IsUnsignedBinaryOperator(expression))
                    {
                        _builder.EmitOpCode(ILOpCode.Rem_un);
                    }
                    else
                    {
                        _builder.EmitOpCode(ILOpCode.Rem);
                    }
                    break;

                case BinaryOperatorKind.LeftShift:
                    _builder.EmitOpCode(ILOpCode.Shl);
                    break;

                case BinaryOperatorKind.RightShift:
                    if (IsUnsignedBinaryOperator(expression))
                    {
                        _builder.EmitOpCode(ILOpCode.Shr_un);
                    }
                    else
                    {
                        _builder.EmitOpCode(ILOpCode.Shr);
                    }
                    break;

                case BinaryOperatorKind.UnsignedRightShift:
                    _builder.EmitOpCode(ILOpCode.Shr_un);
                    break;

                case BinaryOperatorKind.And:
                    _builder.EmitOpCode(ILOpCode.And);
                    break;

                case BinaryOperatorKind.Xor:
                    _builder.EmitOpCode(ILOpCode.Xor);
                    break;

                case BinaryOperatorKind.Or:
                    _builder.EmitOpCode(ILOpCode.Or);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(expression.OperatorKind.Operator());
            }
        }

        private void EmitShortCircuitingOperator(BoundBinaryOperator condition, bool sense, bool stopSense, bool stopValue)
        {
            // we generate:
            //
            // gotoif (a == stopSense) fallThrough
            // b == sense
            // goto labEnd
            // fallThrough:
            // stopValue
            // labEnd:
            //                 AND       OR
            //            +-  ------    -----
            // stopSense  |   !sense    sense
            // stopValue  |     0         1

            object lazyFallThrough = null;

            EmitCondBranch(condition.Left, ref lazyFallThrough, stopSense);
            EmitCondExpr(condition.Right, sense);

            // if fall-through was not initialized, no-one is going to take that branch
            // and we are done with Right on stack
            if (lazyFallThrough == null)
            {
                return;
            }

            var labEnd = new object();
            _builder.EmitBranch(ILOpCode.Br, labEnd);

            // if we get to fallThrough, we should not have Right on stack. Adjust for that.
            _builder.AdjustStack(-1);

            _builder.MarkLabel(lazyFallThrough);
            _builder.EmitBoolConstant(stopValue);
            _builder.MarkLabel(labEnd);
        }

        //NOTE: odd positions assume inverted sense
        private static readonly ILOpCode[] s_compOpCodes = new ILOpCode[]
        {
            //  <            <=               >                >=
            ILOpCode.Clt,    ILOpCode.Cgt,    ILOpCode.Cgt,    ILOpCode.Clt,     // Signed
            ILOpCode.Clt_un, ILOpCode.Cgt_un, ILOpCode.Cgt_un, ILOpCode.Clt_un,  // Unsigned
            ILOpCode.Clt,    ILOpCode.Cgt_un, ILOpCode.Cgt,    ILOpCode.Clt_un,  // Float
        };

        //NOTE: The result of this should be a boolean on the stack.
        private void EmitBinaryCondOperator(BoundBinaryOperator binOp, bool sense)
        {
            bool andOrSense = sense;
            int opIdx;

            switch (binOp.OperatorKind.OperatorWithLogical())
            {
                case BinaryOperatorKind.LogicalOr:
                    Debug.Assert(binOp.Left.Type.SpecialType == SpecialType.System_Boolean);
                    Debug.Assert(binOp.Right.Type.SpecialType == SpecialType.System_Boolean);

                    // Rewrite (a || b) as ~(~a && ~b)
                    andOrSense = !andOrSense;
                    // Fall through
                    goto case BinaryOperatorKind.LogicalAnd;

                case BinaryOperatorKind.LogicalAnd:
                    Debug.Assert(binOp.Left.Type.SpecialType == SpecialType.System_Boolean);
                    Debug.Assert(binOp.Right.Type.SpecialType == SpecialType.System_Boolean);

                    // ~(a && b) is equivalent to (~a || ~b)
                    if (!andOrSense)
                    {
                        // generate (~a || ~b)
                        EmitShortCircuitingOperator(binOp, sense, sense, true);
                    }
                    else
                    {
                        // generate (a && b)
                        EmitShortCircuitingOperator(binOp, sense, !sense, false);
                    }
                    return;

                case BinaryOperatorKind.And:
                    Debug.Assert(binOp.Left.Type.SpecialType == SpecialType.System_Boolean);
                    Debug.Assert(binOp.Right.Type.SpecialType == SpecialType.System_Boolean);
                    EmitBinaryCondOperatorHelper(ILOpCode.And, binOp.Left, binOp.Right, sense);
                    return;

                case BinaryOperatorKind.Or:
                    Debug.Assert(binOp.Left.Type.SpecialType == SpecialType.System_Boolean);
                    Debug.Assert(binOp.Right.Type.SpecialType == SpecialType.System_Boolean);
                    EmitBinaryCondOperatorHelper(ILOpCode.Or, binOp.Left, binOp.Right, sense);
                    return;

                case BinaryOperatorKind.Xor:
                    Debug.Assert(binOp.Left.Type.SpecialType == SpecialType.System_Boolean);
                    Debug.Assert(binOp.Right.Type.SpecialType == SpecialType.System_Boolean);

                    // Xor is equivalent to not equal.
                    if (sense)
                        EmitBinaryCondOperatorHelper(ILOpCode.Xor, binOp.Left, binOp.Right, true);
                    else
                        EmitBinaryCondOperatorHelper(ILOpCode.Ceq, binOp.Left, binOp.Right, true);
                    return;

                case BinaryOperatorKind.NotEqual:
                    // neq  is emitted as  !eq
                    sense = !sense;
                    goto case BinaryOperatorKind.Equal;

                case BinaryOperatorKind.Equal:

                    var constant = binOp.Left.ConstantValueOpt;
                    var comparand = binOp.Right;

                    if (constant == null)
                    {
                        constant = comparand.ConstantValueOpt;
                        comparand = binOp.Left;
                    }

                    if (constant != null)
                    {
                        if (constant.IsDefaultValue)
                        {
                            if (!constant.IsFloating)
                            {
                                if (comparand is BoundConversion { Type.SpecialType: SpecialType.System_Object, ConversionKind: ConversionKind.Boxing, Operand.Type: TypeParameterSymbol { AllowsRefLikeType: true } } &&
                                    constant.IsNull)
                                {
                                    // Boxing is not supported for ref like type parameters, therefore the code that we usually emit 'box; ldnull; ceq/cgt'
                                    // is not going to work. There is, however, an exception for 'box; brtrue/brfalse' sequence (https://github.com/dotnet/runtime/blob/main/docs/design/features/byreflike-generics.md#special-il-sequences).
                                    EmitExpression(comparand, true);

                                    object falseLabel = new object();
                                    object endLabel = new object();
                                    _builder.EmitBranch(sense ? ILOpCode.Brtrue_s : ILOpCode.Brfalse_s, falseLabel);
                                    _builder.EmitBoolConstant(true);
                                    _builder.EmitBranch(ILOpCode.Br, endLabel);

                                    _builder.AdjustStack(-1);
                                    _builder.MarkLabel(falseLabel);
                                    _builder.EmitBoolConstant(false);
                                    _builder.MarkLabel(endLabel);
                                    return;
                                }

                                if (sense)
                                {
                                    EmitIsNullOrZero(comparand, constant);
                                }
                                else
                                {
                                    //  obj != null/0   for pointers and integral numerics is emitted as cgt.un
                                    EmitIsNotNullOrZero(comparand, constant);
                                }
                                return;
                            }
                        }
                        else if (constant.IsBoolean)
                        {
                            // treat  "x = True" ==> "x"
                            EmitExpression(comparand, true);
                            EmitIsSense(sense);
                            return;
                        }
                    }

                    EmitBinaryCondOperatorHelper(ILOpCode.Ceq, binOp.Left, binOp.Right, sense);
                    return;

                case BinaryOperatorKind.LessThan:
                    opIdx = 0;
                    break;

                case BinaryOperatorKind.LessThanOrEqual:
                    opIdx = 1;
                    sense = !sense; // lte is emitted as !gt 
                    break;

                case BinaryOperatorKind.GreaterThan:
                    opIdx = 2;
                    break;

                case BinaryOperatorKind.GreaterThanOrEqual:
                    opIdx = 3;
                    sense = !sense; // gte is emitted as !lt 
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(binOp.OperatorKind.OperatorWithLogical());
            }

            if (IsUnsignedBinaryOperator(binOp))
            {
                opIdx += 4;
            }
            else if (IsFloat(binOp.OperatorKind))
            {
                opIdx += 8;
            }

            EmitBinaryCondOperatorHelper(s_compOpCodes[opIdx], binOp.Left, binOp.Right, sense);
            return;
        }

        private void EmitIsNotNullOrZero(BoundExpression comparand, ConstantValue nullOrZero)
        {
            EmitExpression(comparand, true);

            var comparandType = comparand.Type;
            if (comparandType.IsReferenceType && !comparandType.IsVerifierReference())
            {
                EmitBox(comparandType, comparand.Syntax);
            }

            _builder.EmitConstantValue(nullOrZero, comparand.Syntax);
            _builder.EmitOpCode(ILOpCode.Cgt_un);
        }

        private void EmitIsNullOrZero(BoundExpression comparand, ConstantValue nullOrZero)
        {
            EmitExpression(comparand, true);

            var comparandType = comparand.Type;
            if (comparandType.IsReferenceType && !comparandType.IsVerifierReference())
            {
                EmitBox(comparandType, comparand.Syntax);
            }

            _builder.EmitConstantValue(nullOrZero, comparand.Syntax);
            _builder.EmitOpCode(ILOpCode.Ceq);
        }

        private void EmitBinaryCondOperatorHelper(ILOpCode opCode, BoundExpression left, BoundExpression right, bool sense)
        {
            EmitExpression(left, true);
            EmitExpression(right, true);
            _builder.EmitOpCode(opCode);
            EmitIsSense(sense);
        }

        // generate a conditional (ie, boolean) expression...
        // this will leave a value on the stack which conforms to sense, ie:(condition == sense)
        private void EmitCondExpr(BoundExpression condition, bool sense)
        {
            RemoveNegation(ref condition, ref sense);

            Debug.Assert(condition.Type.SpecialType == SpecialType.System_Boolean);

            var constantValue = condition.ConstantValueOpt;
            if (constantValue != null)
            {
                Debug.Assert(constantValue.Discriminator == ConstantValueTypeDiscriminator.Boolean);
                var constant = constantValue.BooleanValue;
                _builder.EmitBoolConstant(constant == sense);
                return;
            }

            if (condition.Kind == BoundKind.BinaryOperator)
            {
                var binOp = (BoundBinaryOperator)condition;
                if (IsConditional(binOp.OperatorKind))
                {
                    EmitBinaryCondOperator(binOp, sense);
                    return;
                }
            }

            EmitExpression(condition, true);
            EmitIsSense(sense);

            return;
        }

        /// <summary>
        /// Emits boolean expression without branching if possible (i.e., no logical operators, only comparisons).
        /// Leaves a boolean (int32, 0 or 1) value on the stack which conforms to sense, i.e., <c>condition == sense</c>.
        /// </summary>
        private bool TryEmitComparison(BoundExpression condition, bool sense)
        {
            RemoveNegation(ref condition, ref sense);

            Debug.Assert(condition.Type.SpecialType == SpecialType.System_Boolean);

            if (condition.ConstantValueOpt is { } constantValue)
            {
                Debug.Assert(constantValue.Discriminator == ConstantValueTypeDiscriminator.Boolean);
                _builder.EmitBoolConstant(constantValue.BooleanValue == sense);
                return true;
            }

            if (condition is BoundBinaryOperator binOp)
            {
                // Intentionally don't optimize logical operators, they need branches to short-circuit.
                if (binOp.OperatorKind.IsComparison())
                {
                    EmitBinaryCondOperator(binOp, sense: sense);
                    return true;
                }
            }
            else if (condition is BoundIsOperator isOp)
            {
                EmitIsExpression(isOp, used: true, omitBooleanConversion: true);

                // Convert to 1 or 0.
                _builder.EmitOpCode(ILOpCode.Ldnull);
                _builder.EmitOpCode(sense ? ILOpCode.Cgt_un : ILOpCode.Ceq);
                return true;
            }
            else
            {
                EmitExpression(condition, used: true);

                // Convert to 1 or 0 (although `condition` is of type `bool`, it can contain any integer).
                _builder.EmitOpCode(ILOpCode.Ldc_i4_0);
                _builder.EmitOpCode(sense ? ILOpCode.Cgt_un : ILOpCode.Ceq);
                return true;
            }

            return false;
        }

        private static void RemoveNegation(ref BoundExpression condition, ref bool sense)
        {
            while (condition is BoundUnaryOperator unOp)
            {
                Debug.Assert(unOp.OperatorKind == UnaryOperatorKind.BoolLogicalNegation);
                condition = unOp.Operand;
                sense = !sense;
            }
        }

        private void EmitUnaryCheckedOperatorExpression(BoundUnaryOperator expression, bool used)
        {
            Debug.Assert(expression.OperatorKind.Operator() == UnaryOperatorKind.UnaryMinus);
            var type = expression.OperatorKind.OperandTypes();

            // Spec 7.6.2
            // Implementation of unary minus has two overloads:
            //   int operator –(int x)
            //   long operator –(long x)
            // 
            // The result is computed by subtracting x from zero. 
            // If the value of x is the smallest representable value of the operand type (−2^31 for int or −2^63 for long),
            // then the mathematical negation of x is not representable within the operand type. If this occurs within a checked context, 
            // a System.OverflowException is thrown; if it occurs within an unchecked context, 
            // the result is the value of the operand and the overflow is not reported.
            Debug.Assert(type == UnaryOperatorKind.Int || type == UnaryOperatorKind.Long || type == UnaryOperatorKind.NInt);

            // ldc.i4.0
            // conv.i8  (when the operand is 64bit)
            // <expr>
            // sub.ovf

            _builder.EmitOpCode(ILOpCode.Ldc_i4_0);

            if (type == UnaryOperatorKind.Long)
            {
                _builder.EmitOpCode(ILOpCode.Conv_i8);
            }
            else if (type == UnaryOperatorKind.NInt)
            {
                _builder.EmitOpCode(ILOpCode.Conv_i);
            }

            EmitExpression(expression.Operand, used: true);
            _builder.EmitOpCode(ILOpCode.Sub_ovf);

            EmitPopIfUnused(used);
        }

        private void EmitConversionToEnumUnderlyingType(BoundBinaryOperator expression, bool @checked)
        {
            // If we are doing an enum addition or subtraction and the 
            // underlying type is 8 or 16 bits then we will have done the operation in 32 
            // bits and we need to convert back down to the smaller bit size
            // to [one|zero]extend the value
            // NOTE: we do not need to do this for bitwise operations since they will always 
            //       result in a properly sign-extended result, assuming operands were sign extended
            //
            // If e is a value of enum type E and u is a value of underlying type u then:
            //
            // e + u --> (E)((U)e + u)
            // u + e --> (E)(u + (U)e)
            // e - e --> (U)((U)e - (U)e)
            // e - u --> (E)((U)e - u)
            // e & e --> (E)((U)e & (U)e)
            // e | e --> (E)((U)e | (U)e)
            // e ^ e --> (E)((U)e ^ (U)e)
            //
            // NOTE: (E) is actually emitted as (U) and in last 3 cases is not necessary.
            //
            // Due to a bug, the native compiler allows:
            //
            // u - e --> (E)(u - (U)e)
            //
            // And so Roslyn does as well.

            TypeSymbol enumType;

            switch (expression.OperatorKind.Operator() | expression.OperatorKind.OperandTypes())
            {
                case BinaryOperatorKind.EnumAndUnderlyingAddition:
                case BinaryOperatorKind.EnumSubtraction:
                case BinaryOperatorKind.EnumAndUnderlyingSubtraction:
                    enumType = expression.Left.Type;
                    break;
                case BinaryOperatorKind.EnumAnd:
                case BinaryOperatorKind.EnumOr:
                case BinaryOperatorKind.EnumXor:
                    Debug.Assert(TypeSymbol.Equals(expression.Left.Type, expression.Right.Type, TypeCompareKind.ConsiderEverything2));
                    enumType = null;
                    break;
                case BinaryOperatorKind.UnderlyingAndEnumSubtraction:
                case BinaryOperatorKind.UnderlyingAndEnumAddition:
                    enumType = expression.Right.Type;
                    break;
                default:
                    enumType = null;
                    break;
            }

            if ((object)enumType == null)
            {
                return;
            }

            Debug.Assert(enumType.IsEnumType());

            SpecialType type = enumType.GetEnumUnderlyingType().SpecialType;
            switch (type)
            {
                case SpecialType.System_Byte:
                    _builder.EmitNumericConversion(Microsoft.Cci.PrimitiveTypeCode.Int32, Microsoft.Cci.PrimitiveTypeCode.UInt8, @checked);
                    break;
                case SpecialType.System_SByte:
                    _builder.EmitNumericConversion(Microsoft.Cci.PrimitiveTypeCode.Int32, Microsoft.Cci.PrimitiveTypeCode.Int8, @checked);
                    break;
                case SpecialType.System_Int16:
                    _builder.EmitNumericConversion(Microsoft.Cci.PrimitiveTypeCode.Int32, Microsoft.Cci.PrimitiveTypeCode.Int16, @checked);
                    break;
                case SpecialType.System_UInt16:
                    _builder.EmitNumericConversion(Microsoft.Cci.PrimitiveTypeCode.Int32, Microsoft.Cci.PrimitiveTypeCode.UInt16, @checked);
                    break;
            }
        }

        private void EmitBinaryCheckedOperatorInstruction(BoundBinaryOperator expression)
        {
            var unsigned = IsUnsignedBinaryOperator(expression);

            switch (expression.OperatorKind.Operator())
            {
                case BinaryOperatorKind.Multiplication:
                    if (unsigned)
                    {
                        _builder.EmitOpCode(ILOpCode.Mul_ovf_un);
                    }
                    else
                    {
                        _builder.EmitOpCode(ILOpCode.Mul_ovf);
                    }
                    break;

                case BinaryOperatorKind.Addition:
                    if (unsigned)
                    {
                        _builder.EmitOpCode(ILOpCode.Add_ovf_un);
                    }
                    else
                    {
                        _builder.EmitOpCode(ILOpCode.Add_ovf);
                    }
                    break;

                case BinaryOperatorKind.Subtraction:
                    if (unsigned)
                    {
                        _builder.EmitOpCode(ILOpCode.Sub_ovf_un);
                    }
                    else
                    {
                        _builder.EmitOpCode(ILOpCode.Sub_ovf);
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(expression.OperatorKind.Operator());
            }
        }

        private static bool OperatorHasSideEffects(BinaryOperatorKind kind)
        {
            switch (kind.Operator())
            {
                case BinaryOperatorKind.Division:
                case BinaryOperatorKind.Remainder:
                    return true;
                default:
                    return kind.IsChecked();
            }
        }

        // emits IsTrue/IsFalse according to the sense
        // IsTrue actually does nothing
        private void EmitIsSense(bool sense)
        {
            if (!sense)
            {
                _builder.EmitOpCode(ILOpCode.Ldc_i4_0);
                _builder.EmitOpCode(ILOpCode.Ceq);
            }
        }

        private static bool IsUnsigned(SpecialType type)
        {
            switch (type)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                    return true;
            }
            return false;
        }

        private static bool IsUnsignedBinaryOperator(BoundBinaryOperator op)
        {
            BinaryOperatorKind opKind = op.OperatorKind;
            Debug.Assert(opKind.Operator() != BinaryOperatorKind.UnsignedRightShift);

            BinaryOperatorKind type = opKind.OperandTypes();
            switch (type)
            {
                case BinaryOperatorKind.Enum:
                case BinaryOperatorKind.EnumAndUnderlying:
                    return IsUnsigned(Binder.GetEnumPromotedType(op.Left.Type.GetEnumUnderlyingType().SpecialType));

                case BinaryOperatorKind.UnderlyingAndEnum:
                    return IsUnsigned(Binder.GetEnumPromotedType(op.Right.Type.GetEnumUnderlyingType().SpecialType));

                case BinaryOperatorKind.UInt:
                case BinaryOperatorKind.NUInt:
                case BinaryOperatorKind.ULong:
                case BinaryOperatorKind.ULongAndPointer:
                case BinaryOperatorKind.PointerAndInt:
                case BinaryOperatorKind.PointerAndUInt:
                case BinaryOperatorKind.PointerAndLong:
                case BinaryOperatorKind.PointerAndULong:
                case BinaryOperatorKind.Pointer:
                    return true;

                // Dev10 bases signedness on the first operand (see ILGENREC::genOperatorExpr).
                case BinaryOperatorKind.IntAndPointer:
                case BinaryOperatorKind.LongAndPointer:
                // Dev10 converts the uint to a native int, so it counts as signed.
                case BinaryOperatorKind.UIntAndPointer:
                default:
                    return false;
            }
        }

        private static bool IsConditional(BinaryOperatorKind opKind)
        {
            switch (opKind.OperatorWithLogical())
            {
                case BinaryOperatorKind.LogicalAnd:
                case BinaryOperatorKind.LogicalOr:
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.LessThanOrEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                    return true;

                case BinaryOperatorKind.And:
                case BinaryOperatorKind.Or:
                case BinaryOperatorKind.Xor:
                    return opKind.OperandTypes() == BinaryOperatorKind.Bool;
            }

            return false;
        }

        private static bool IsFloat(BinaryOperatorKind opKind)
        {
            var type = opKind.OperandTypes();
            switch (type)
            {
                case BinaryOperatorKind.Float:
                case BinaryOperatorKind.Double:
                    return true;
                default:
                    return false;
            }
        }
    }
}
