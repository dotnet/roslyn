// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.RuntimeMembers;
using System.Collections.Generic;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
        {
            return VisitBinaryOperator(node, null);
        }

        public override BoundNode VisitUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator node)
        {
            // Yes, we could have a lifted, logical, user-defined operator:
            //
            // struct C { 
            //   public static C operator &(C x, C y) {...}
            //   public static bool operator true(C? c) { ... }
            //   public static bool operator false(C? c) { ... }
            // }
            //
            // If we have C? q, r and we say q && r then this gets bound as 
            // C? tempQ = q ;
            // C.false(tempQ) ? 
            //     tempQ : 
            //     ( 
            //         C? tempR = r ; 
            //         tempQ.HasValue & tempR.HasValue ? 
            //           new C?(C.&(tempQ.GetValueOrDefault(), tempR.GetValueOrDefault())) :
            //           default C?()
            //     )
            //
            // Note that the native compiler does not allow q && r. However, the native compiler
            // *does* allow q && r if C is defined as:
            //
            // struct C { 
            //   public static C? operator &(C? x, C? y) {...}
            //   public static bool operator true(C? c) { ... }
            //   public static bool operator false(C? c) { ... }
            // }
            //
            // It seems unusual and wrong that an & operator should be allowed to become
            // a && operator if there is a "manually lifted" operator in source, but not
            // if there is a "synthesized" lifted operator.  Roslyn fixes this bug.
            //
            // Anyway, in this case we must lower this to its non-logical form, and then
            // lower the interior of that to its non-lifted form.

            // See comments in method IsValidUserDefinedConditionalLogicalOperator for information
            // on some subtle aspects of this lowering.

            // We generate one of:
            //
            // x || y --> temp = x; T.true(temp)  ? temp : T.|(temp, y);
            // x && y --> temp = x; T.false(temp) ? temp : T.&(temp, y);
            //
            // For the ease of naming locals, we'll assume we're doing an &&.

            // TODO: We generate every one of these as "temp = x; T.false(temp) ? temp : T.&(temp, y)" even
            // TODO: when x has no side effects. We can optimize away the temporary if there are no side effects.

            var syntax = node.Syntax;
            var operatorKind = node.OperatorKind;
            var type = node.Type;

            BoundExpression loweredLeft = VisitExpression(node.Left);
            BoundExpression loweredRight = VisitExpression(node.Right);

            if (_inExpressionLambda)
            {
                return node.Update(operatorKind, node.LogicalOperator, node.TrueOperator, node.FalseOperator, node.ResultKind, loweredLeft, loweredRight, type);
            }

            BoundAssignmentOperator tempAssignment;
            var boundTemp = _factory.StoreToTemp(loweredLeft, out tempAssignment);

            // T.false(temp)
            var falseOperatorCall = BoundCall.Synthesized(syntax, null, operatorKind.Operator() == BinaryOperatorKind.And ? node.FalseOperator : node.TrueOperator, boundTemp);

            // T.&(temp, y)
            var andOperatorCall = LowerUserDefinedBinaryOperator(syntax, operatorKind & ~BinaryOperatorKind.Logical, boundTemp, loweredRight, type, node.LogicalOperator);

            // T.false(temp) ? temp : T.&(temp, y)
            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: syntax,
                rewrittenCondition: falseOperatorCall,
                rewrittenConsequence: boundTemp,
                rewrittenAlternative: andOperatorCall,
                constantValueOpt: null,
                rewrittenType: type,
                isRef: false);

            // temp = x; T.false(temp) ? temp : T.&(temp, y)
            return new BoundSequence(
                syntax: syntax,
                locals: ImmutableArray.Create(boundTemp.LocalSymbol),
                sideEffects: ImmutableArray.Create<BoundExpression>(tempAssignment),
                value: conditionalExpression,
                type: type);
        }

        public BoundExpression VisitBinaryOperator(BoundBinaryOperator node, BoundUnaryOperator applyParentUnaryOperator)
        {
            // In machine-generated code we frequently end up with binary operator trees that are deep on the left,
            // such as a + b + c + d ...
            // To avoid blowing the call stack, we make an explicit stack of the binary operators to the left, 
            // and then lower by traversing the explicit stack.

            var stack = ArrayBuilder<BoundBinaryOperator>.GetInstance();

            for (BoundBinaryOperator current = node; current != null && current.ConstantValue == null; current = current.Left as BoundBinaryOperator)
            {
                stack.Push(current);
            }

            BoundExpression loweredLeft = VisitExpression(stack.Peek().Left);
            while (stack.Count > 0)
            {
                BoundBinaryOperator original = stack.Pop();
                BoundExpression loweredRight = VisitExpression(original.Right);
                loweredLeft = MakeBinaryOperator(original, original.Syntax, original.OperatorKind, loweredLeft, loweredRight, original.Type, original.MethodOpt,
                    applyParentUnaryOperator: (stack.Count == 0) ? applyParentUnaryOperator : null);
            }

            stack.Free();
            return loweredLeft;
        }

        private BoundExpression MakeBinaryOperator(
            SyntaxNode syntax,
            BinaryOperatorKind operatorKind,
            BoundExpression loweredLeft,
            BoundExpression loweredRight,
            TypeSymbol type,
            MethodSymbol method,
            bool isPointerElementAccess = false,
            bool isCompoundAssignment = false,
            BoundUnaryOperator applyParentUnaryOperator = null)
        {
            return MakeBinaryOperator(null, syntax, operatorKind, loweredLeft, loweredRight, type, method, isPointerElementAccess, isCompoundAssignment, applyParentUnaryOperator);
        }

        private BoundExpression MakeBinaryOperator(
            BoundBinaryOperator oldNode,
            SyntaxNode syntax,
            BinaryOperatorKind operatorKind,
            BoundExpression loweredLeft,
            BoundExpression loweredRight,
            TypeSymbol type,
            MethodSymbol method,
            bool isPointerElementAccess = false,
            bool isCompoundAssignment = false,
            BoundUnaryOperator applyParentUnaryOperator = null)
        {
            Debug.Assert(oldNode == null || (oldNode.Syntax == syntax));

            if (_inExpressionLambda)
            {
                switch (operatorKind.Operator() | operatorKind.OperandTypes())
                {
                    case BinaryOperatorKind.ObjectAndStringConcatenation:
                    case BinaryOperatorKind.StringAndObjectConcatenation:
                    case BinaryOperatorKind.StringConcatenation:
                        return RewriteStringConcatenation(syntax, operatorKind, loweredLeft, loweredRight, type);
                    case BinaryOperatorKind.DelegateCombination:
                        return RewriteDelegateOperation(syntax, operatorKind, loweredLeft, loweredRight, type, SpecialMember.System_Delegate__Combine);
                    case BinaryOperatorKind.DelegateRemoval:
                        return RewriteDelegateOperation(syntax, operatorKind, loweredLeft, loweredRight, type, SpecialMember.System_Delegate__Remove);
                    case BinaryOperatorKind.DelegateEqual:
                        return RewriteDelegateOperation(syntax, operatorKind, loweredLeft, loweredRight, type, SpecialMember.System_Delegate__op_Equality);
                    case BinaryOperatorKind.DelegateNotEqual:
                        return RewriteDelegateOperation(syntax, operatorKind, loweredLeft, loweredRight, type, SpecialMember.System_Delegate__op_Inequality);
                }
            }
            else
            // try to lower the expression.
            {
                if (operatorKind.IsDynamic())
                {
                    Debug.Assert(!isPointerElementAccess);

                    if (operatorKind.IsLogical())
                    {
                        return MakeDynamicLogicalBinaryOperator(syntax, operatorKind, loweredLeft, loweredRight, method, type, isCompoundAssignment, applyParentUnaryOperator);
                    }
                    else
                    {
                        Debug.Assert((object)method == null);
                        return _dynamicFactory.MakeDynamicBinaryOperator(operatorKind, loweredLeft, loweredRight, isCompoundAssignment, type).ToExpression();
                    }
                }

                if (operatorKind.IsLifted())
                {
                    return RewriteLiftedBinaryOperator(syntax, operatorKind, loweredLeft, loweredRight, type, method);
                }

                if (operatorKind.IsUserDefined())
                {
                    return LowerUserDefinedBinaryOperator(syntax, operatorKind, loweredLeft, loweredRight, type, method);
                }

                switch (operatorKind.OperatorWithLogical() | operatorKind.OperandTypes())
                {
                    case BinaryOperatorKind.NullableNullEqual:
                    case BinaryOperatorKind.NullableNullNotEqual:
                        return RewriteNullableNullEquality(syntax, operatorKind, loweredLeft, loweredRight, type);

                    case BinaryOperatorKind.ObjectAndStringConcatenation:
                    case BinaryOperatorKind.StringAndObjectConcatenation:
                    case BinaryOperatorKind.StringConcatenation:
                        return RewriteStringConcatenation(syntax, operatorKind, loweredLeft, loweredRight, type);

                    case BinaryOperatorKind.StringEqual:
                        return RewriteStringEquality(oldNode, syntax, operatorKind, loweredLeft, loweredRight, type, SpecialMember.System_String__op_Equality);

                    case BinaryOperatorKind.StringNotEqual:
                        return RewriteStringEquality(oldNode, syntax, operatorKind, loweredLeft, loweredRight, type, SpecialMember.System_String__op_Inequality);

                    case BinaryOperatorKind.DelegateCombination:
                        return RewriteDelegateOperation(syntax, operatorKind, loweredLeft, loweredRight, type, SpecialMember.System_Delegate__Combine);

                    case BinaryOperatorKind.DelegateRemoval:
                        return RewriteDelegateOperation(syntax, operatorKind, loweredLeft, loweredRight, type, SpecialMember.System_Delegate__Remove);

                    case BinaryOperatorKind.DelegateEqual:
                        return RewriteDelegateOperation(syntax, operatorKind, loweredLeft, loweredRight, type, SpecialMember.System_Delegate__op_Equality);

                    case BinaryOperatorKind.DelegateNotEqual:
                        return RewriteDelegateOperation(syntax, operatorKind, loweredLeft, loweredRight, type, SpecialMember.System_Delegate__op_Inequality);

                    case BinaryOperatorKind.LogicalBoolAnd:
                        if (loweredRight.ConstantValue == ConstantValue.True) return loweredLeft;
                        if (loweredLeft.ConstantValue == ConstantValue.True) return loweredRight;
                        if (loweredLeft.ConstantValue == ConstantValue.False) return loweredLeft;

                        if (loweredRight.Kind == BoundKind.Local || loweredRight.Kind == BoundKind.Parameter)
                        {
                            operatorKind &= ~BinaryOperatorKind.Logical;
                        }

                        goto default;

                    case BinaryOperatorKind.LogicalBoolOr:
                        if (loweredRight.ConstantValue == ConstantValue.False) return loweredLeft;
                        if (loweredLeft.ConstantValue == ConstantValue.False) return loweredRight;
                        if (loweredLeft.ConstantValue == ConstantValue.True) return loweredLeft;

                        if (loweredRight.Kind == BoundKind.Local || loweredRight.Kind == BoundKind.Parameter)
                        {
                            operatorKind &= ~BinaryOperatorKind.Logical;
                        }

                        goto default;

                    case BinaryOperatorKind.BoolAnd:
                        if (loweredRight.ConstantValue == ConstantValue.True) return loweredLeft;
                        if (loweredLeft.ConstantValue == ConstantValue.True) return loweredRight;

                        // Note that we are using IsDefaultValue instead of False.
                        // That is just to catch cases like default(bool) or others resulting in 
                        // a default bool value, that we know to be "false"
                        // bool? generally should not reach here, since it is handled by RewriteLiftedBinaryOperator.
                        // Regardless, the following code should handle default(bool?) correctly since
                        // default(bool?) & <expr> == default(bool?)  with sideeffects of <expr>
                        if (loweredLeft.IsDefaultValue())
                        {
                            return _factory.MakeSequence(loweredRight, loweredLeft);
                        }
                        if (loweredRight.IsDefaultValue())
                        {
                            return _factory.MakeSequence(loweredLeft, loweredRight);
                        }

                        goto default;

                    case BinaryOperatorKind.BoolOr:
                        if (loweredRight.ConstantValue == ConstantValue.False) return loweredLeft;
                        if (loweredLeft.ConstantValue == ConstantValue.False) return loweredRight;
                        goto default;

                    case BinaryOperatorKind.BoolEqual:
                        if (loweredLeft.ConstantValue == ConstantValue.True) return loweredRight;
                        if (loweredRight.ConstantValue == ConstantValue.True) return loweredLeft;

                        if (loweredLeft.ConstantValue == ConstantValue.False)
                            return MakeUnaryOperator(UnaryOperatorKind.BoolLogicalNegation, syntax, null, loweredRight, loweredRight.Type);

                        if (loweredRight.ConstantValue == ConstantValue.False)
                            return MakeUnaryOperator(UnaryOperatorKind.BoolLogicalNegation, syntax, null, loweredLeft, loweredLeft.Type);

                        goto default;

                    case BinaryOperatorKind.BoolNotEqual:
                        if (loweredLeft.ConstantValue == ConstantValue.False) return loweredRight;
                        if (loweredRight.ConstantValue == ConstantValue.False) return loweredLeft;

                        if (loweredLeft.ConstantValue == ConstantValue.True)
                            return MakeUnaryOperator(UnaryOperatorKind.BoolLogicalNegation, syntax, null, loweredRight, loweredRight.Type);

                        if (loweredRight.ConstantValue == ConstantValue.True)
                            return MakeUnaryOperator(UnaryOperatorKind.BoolLogicalNegation, syntax, null, loweredLeft, loweredLeft.Type);

                        goto default;

                    case BinaryOperatorKind.BoolXor:
                        if (loweredLeft.ConstantValue == ConstantValue.False) return loweredRight;
                        if (loweredRight.ConstantValue == ConstantValue.False) return loweredLeft;

                        if (loweredLeft.ConstantValue == ConstantValue.True)
                            return MakeUnaryOperator(UnaryOperatorKind.BoolLogicalNegation, syntax, null, loweredRight, loweredRight.Type);

                        if (loweredRight.ConstantValue == ConstantValue.True)
                            return MakeUnaryOperator(UnaryOperatorKind.BoolLogicalNegation, syntax, null, loweredLeft, loweredLeft.Type);

                        goto default;

                    case BinaryOperatorKind.IntLeftShift:
                    case BinaryOperatorKind.UIntLeftShift:
                    case BinaryOperatorKind.IntRightShift:
                    case BinaryOperatorKind.UIntRightShift:
                        return RewriteBuiltInShiftOperation(oldNode, syntax, operatorKind, loweredLeft, loweredRight, type, 0x1F);

                    case BinaryOperatorKind.LongLeftShift:
                    case BinaryOperatorKind.ULongLeftShift:
                    case BinaryOperatorKind.LongRightShift:
                    case BinaryOperatorKind.ULongRightShift:
                        return RewriteBuiltInShiftOperation(oldNode, syntax, operatorKind, loweredLeft, loweredRight, type, 0x3F);

                    case BinaryOperatorKind.DecimalAddition:
                    case BinaryOperatorKind.DecimalSubtraction:
                    case BinaryOperatorKind.DecimalMultiplication:
                    case BinaryOperatorKind.DecimalDivision:
                    case BinaryOperatorKind.DecimalRemainder:
                    case BinaryOperatorKind.DecimalEqual:
                    case BinaryOperatorKind.DecimalNotEqual:
                    case BinaryOperatorKind.DecimalLessThan:
                    case BinaryOperatorKind.DecimalLessThanOrEqual:
                    case BinaryOperatorKind.DecimalGreaterThan:
                    case BinaryOperatorKind.DecimalGreaterThanOrEqual:
                        return RewriteDecimalBinaryOperation(syntax, loweredLeft, loweredRight, operatorKind);

                    case BinaryOperatorKind.PointerAndIntAddition:
                    case BinaryOperatorKind.PointerAndUIntAddition:
                    case BinaryOperatorKind.PointerAndLongAddition:
                    case BinaryOperatorKind.PointerAndULongAddition:
                    case BinaryOperatorKind.PointerAndIntSubtraction:
                    case BinaryOperatorKind.PointerAndUIntSubtraction:
                    case BinaryOperatorKind.PointerAndLongSubtraction:
                    case BinaryOperatorKind.PointerAndULongSubtraction:
                        if (loweredRight.IsDefaultValue())
                        {
                            return loweredLeft;
                        }
                        return RewritePointerNumericOperator(syntax, operatorKind, loweredLeft, loweredRight, type, isPointerElementAccess, isLeftPointer: true);

                    case BinaryOperatorKind.IntAndPointerAddition:
                    case BinaryOperatorKind.UIntAndPointerAddition:
                    case BinaryOperatorKind.LongAndPointerAddition:
                    case BinaryOperatorKind.ULongAndPointerAddition:
                        if (loweredLeft.IsDefaultValue())
                        {
                            return loweredRight;
                        }
                        return RewritePointerNumericOperator(syntax, operatorKind, loweredLeft, loweredRight, type, isPointerElementAccess, isLeftPointer: false);

                    case BinaryOperatorKind.PointerSubtraction:
                        return RewritePointerSubtraction(operatorKind, loweredLeft, loweredRight, type);

                    case BinaryOperatorKind.IntAddition:
                    case BinaryOperatorKind.UIntAddition:
                    case BinaryOperatorKind.LongAddition:
                    case BinaryOperatorKind.ULongAddition:
                        if (loweredLeft.IsDefaultValue())
                        {
                            return loweredRight;
                        }
                        if (loweredRight.IsDefaultValue())
                        {
                            return loweredLeft;
                        }
                        goto default;

                    case BinaryOperatorKind.IntSubtraction:
                    case BinaryOperatorKind.LongSubtraction:
                    case BinaryOperatorKind.UIntSubtraction:
                    case BinaryOperatorKind.ULongSubtraction:
                        if (loweredRight.IsDefaultValue())
                        {
                            return loweredLeft;
                        }
                        goto default;

                    case BinaryOperatorKind.IntMultiplication:
                    case BinaryOperatorKind.LongMultiplication:
                    case BinaryOperatorKind.UIntMultiplication:
                    case BinaryOperatorKind.ULongMultiplication:
                        if (loweredLeft.IsDefaultValue())
                        {
                            return _factory.MakeSequence(loweredRight, loweredLeft);
                        }
                        if (loweredRight.IsDefaultValue())
                        {
                            return _factory.MakeSequence(loweredLeft, loweredRight);
                        }
                        if (loweredLeft.ConstantValue?.UInt64Value == 1)
                        {
                            return loweredRight;
                        }
                        if (loweredRight.ConstantValue?.UInt64Value == 1)
                        {
                            return loweredLeft;
                        }
                        goto default;

                    case BinaryOperatorKind.IntGreaterThan:
                    case BinaryOperatorKind.IntLessThanOrEqual:
                        if (loweredLeft.Kind == BoundKind.ArrayLength && loweredRight.IsDefaultValue())
                        {
                            //array length is never negative
                            var newOp = operatorKind == BinaryOperatorKind.IntGreaterThan ?
                                                        BinaryOperatorKind.NotEqual :
                                                        BinaryOperatorKind.Equal;

                            operatorKind &= ~BinaryOperatorKind.OpMask;
                            operatorKind |= newOp;
                            loweredLeft = UnconvertArrayLength((BoundArrayLength)loweredLeft);
                        }
                        goto default;

                    case BinaryOperatorKind.IntLessThan:
                    case BinaryOperatorKind.IntGreaterThanOrEqual:
                        if (loweredRight.Kind == BoundKind.ArrayLength && loweredLeft.IsDefaultValue())
                        {
                            //array length is never negative
                            var newOp = operatorKind == BinaryOperatorKind.IntLessThan ?
                                                        BinaryOperatorKind.NotEqual :
                                                        BinaryOperatorKind.Equal;

                            operatorKind &= ~BinaryOperatorKind.OpMask;
                            operatorKind |= newOp;
                            loweredRight = UnconvertArrayLength((BoundArrayLength)loweredRight);
                        }
                        goto default;

                    case BinaryOperatorKind.IntEqual:
                    case BinaryOperatorKind.IntNotEqual:
                        if (loweredLeft.Kind == BoundKind.ArrayLength && loweredRight.IsDefaultValue())
                        {
                            loweredLeft = UnconvertArrayLength((BoundArrayLength)loweredLeft);
                        }
                        else if (loweredRight.Kind == BoundKind.ArrayLength && loweredLeft.IsDefaultValue())
                        {
                            loweredRight = UnconvertArrayLength((BoundArrayLength)loweredRight);
                        }

                        goto default;

                    default:
                        break;
                }
            }

            return (oldNode != null) ?
                oldNode.Update(operatorKind, oldNode.ConstantValueOpt, oldNode.MethodOpt, oldNode.ResultKind, loweredLeft, loweredRight, type) :
                new BoundBinaryOperator(syntax, operatorKind, null, null, LookupResultKind.Viable, loweredLeft, loweredRight, type);
        }

        private BoundExpression RewriteLiftedBinaryOperator(SyntaxNode syntax, BinaryOperatorKind operatorKind, BoundExpression loweredLeft, BoundExpression loweredRight, TypeSymbol type, MethodSymbol method)
        {
            var conditionalLeft = loweredLeft as BoundLoweredConditionalAccess;

            // NOTE: we could in theory handle side-effecting loweredRight here too
            //       by including it as a part of whenNull, but there is a concern 
            //       that it can lead to code duplication
            var optimize = conditionalLeft != null &&
                !ReadIsSideeffecting(loweredRight) &&
                (conditionalLeft.WhenNullOpt == null || conditionalLeft.WhenNullOpt.IsDefaultValue());

            if (optimize)
            {
                loweredLeft = conditionalLeft.WhenNotNull;
            }

            var result = operatorKind.IsComparison() ?
                            operatorKind.IsUserDefined() ?
                                LowerLiftedUserDefinedComparisonOperator(syntax, operatorKind, loweredLeft, loweredRight, method) :
                                LowerLiftedBuiltInComparisonOperator(syntax, operatorKind, loweredLeft, loweredRight) :
                            LowerLiftedBinaryArithmeticOperator(syntax, operatorKind, loweredLeft, loweredRight, type, method);

            if (optimize)
            {
                BoundExpression whenNullOpt = null;

                // for all operators null-in means null-out
                // except for the Equal/NotEqual since null == null ==> true
                if (operatorKind.Operator() == BinaryOperatorKind.NotEqual ||
                    operatorKind.Operator() == BinaryOperatorKind.Equal)
                {
                    whenNullOpt = RewriteLiftedBinaryOperator(syntax, operatorKind, _factory.Default(loweredLeft.Type), loweredRight, type, method);
                }

                result = conditionalLeft.Update(
                    conditionalLeft.Receiver,
                    conditionalLeft.HasValueMethodOpt,
                    whenNotNull: result,
                    whenNullOpt: whenNullOpt,
                    id: conditionalLeft.Id,
                    type: result.Type
                );
            }

            return result;
        }


        //array length produces native uint, so the node typically implies a conversion to int32/int64.  
        //Sometimes the conversion is not necessary - i.e. when we just check for 0
        //This helper removes unnecessary implied conversion from ArrayLength node.
        private BoundExpression UnconvertArrayLength(BoundArrayLength arrLength)
        {
            return arrLength.Update(arrLength.Expression, _factory.SpecialType(SpecialType.System_UIntPtr));
        }

        private BoundExpression MakeDynamicLogicalBinaryOperator(
            SyntaxNode syntax,
            BinaryOperatorKind operatorKind,
            BoundExpression loweredLeft,
            BoundExpression loweredRight,
            MethodSymbol leftTruthOperator,
            TypeSymbol type,
            bool isCompoundAssignment,
            BoundUnaryOperator applyParentUnaryOperator)
        {
            Debug.Assert(operatorKind.Operator() == BinaryOperatorKind.And || operatorKind.Operator() == BinaryOperatorKind.Or);

            // Dynamic logical && and || operators are lowered as follows:
            //   left && right  ->  IsFalse(left) ? left : And(left, right)
            //   left || right  ->  IsTrue(left) ? left : Or(left, right)
            // 
            // Optimization: If the binary AND/OR is directly contained in IsFalse/IsTrue operator (parentUnaryOperator != null)
            // we can avoid calling IsFalse/IsTrue twice on the same object.
            //   IsFalse(left && right)  ->  IsFalse(left) || IsFalse(And(left, right))
            //   IsTrue(left || right)   ->  IsTrue(left) || IsTrue(Or(left, right))

            bool isAnd = operatorKind.Operator() == BinaryOperatorKind.And;

            // Operator to be used to test the left operand:
            var testOperator = isAnd ? UnaryOperatorKind.DynamicFalse : UnaryOperatorKind.DynamicTrue;

            // VisitUnaryOperator ensures we are never called with parentUnaryOperator != null when we can't perform the optimization.
            Debug.Assert(applyParentUnaryOperator == null || applyParentUnaryOperator.OperatorKind == testOperator);

            ConstantValue constantLeft = loweredLeft.ConstantValue ?? UnboxConstant(loweredLeft);
            if (testOperator == UnaryOperatorKind.DynamicFalse && constantLeft == ConstantValue.False ||
                testOperator == UnaryOperatorKind.DynamicTrue && constantLeft == ConstantValue.True)
            {
                Debug.Assert(leftTruthOperator == null);

                if (applyParentUnaryOperator != null)
                {
                    // IsFalse(false && right) -> true
                    // IsTrue(true || right)   -> true
                    return _factory.Literal(true);
                }
                else
                {
                    // false && right  ->  box(false)
                    // true || right   ->  box(true)
                    return MakeConversionNode(loweredLeft, type, @checked: false);
                }
            }

            BoundExpression result;
            var boolean = _compilation.GetSpecialType(SpecialType.System_Boolean);

            // Store left to local if needed. If constant or already local we don't need a temp 
            // since the value of left can't change until right is evaluated.
            BoundAssignmentOperator tempAssignment;
            BoundLocal temp;
            if (constantLeft == null && loweredLeft.Kind != BoundKind.Local && loweredLeft.Kind != BoundKind.Parameter)
            {
                BoundAssignmentOperator assignment;
                var local = _factory.StoreToTemp(loweredLeft, out assignment);
                loweredLeft = local;
                tempAssignment = assignment;
                temp = local;
            }
            else
            {
                tempAssignment = null;
                temp = null;
            }

            var op = _dynamicFactory.MakeDynamicBinaryOperator(operatorKind, loweredLeft, loweredRight, isCompoundAssignment, type).ToExpression();

            // IsFalse(true) or IsTrue(false) are always false:
            bool leftTestIsConstantFalse = testOperator == UnaryOperatorKind.DynamicFalse && constantLeft == ConstantValue.True ||
                                           testOperator == UnaryOperatorKind.DynamicTrue && constantLeft == ConstantValue.False;

            if (applyParentUnaryOperator != null)
            {
                // IsFalse(left && right)  ->  IsFalse(left) || IsFalse(And(left, right))
                // IsTrue(left || right)   ->  IsTrue(left) || IsTrue(Or(left, right))

                result = _dynamicFactory.MakeDynamicUnaryOperator(testOperator, op, boolean).ToExpression();
                if (!leftTestIsConstantFalse)
                {
                    BoundExpression leftTest = MakeTruthTestForDynamicLogicalOperator(syntax, loweredLeft, boolean, leftTruthOperator, negative: isAnd);
                    result = _factory.Binary(BinaryOperatorKind.LogicalOr, boolean, leftTest, result);
                }
            }
            else
            {
                // left && right  ->  IsFalse(left) ? left : And(left, right)
                // left || right  ->  IsTrue(left) ? left : Or(left, right)

                if (leftTestIsConstantFalse)
                {
                    result = op;
                }
                else
                {
                    // We might need to box.
                    BoundExpression leftTest = MakeTruthTestForDynamicLogicalOperator(syntax, loweredLeft, boolean, leftTruthOperator, negative: isAnd);
                    var convertedLeft = MakeConversionNode(loweredLeft, type, @checked: false);
                    result = _factory.Conditional(leftTest, convertedLeft, op, type);
                }
            }

            if (tempAssignment != null)
            {
                return _factory.Sequence(ImmutableArray.Create(temp.LocalSymbol), ImmutableArray.Create<BoundExpression>(tempAssignment), result);
            }

            return result;
        }

        private static ConstantValue UnboxConstant(BoundExpression expression)
        {
            if (expression.Kind == BoundKind.Conversion)
            {
                var conversion = (BoundConversion)expression;
                if (conversion.ConversionKind == ConversionKind.Boxing)
                {
                    return conversion.Operand.ConstantValue;
                }
            }

            return null;
        }

        private BoundExpression MakeTruthTestForDynamicLogicalOperator(SyntaxNode syntax, BoundExpression loweredLeft, TypeSymbol boolean, MethodSymbol leftTruthOperator, bool negative)
        {
            if (loweredLeft.HasDynamicType())
            {
                Debug.Assert(leftTruthOperator == null);
                return _dynamicFactory.MakeDynamicUnaryOperator(negative ? UnaryOperatorKind.DynamicFalse : UnaryOperatorKind.DynamicTrue, loweredLeft, boolean).ToExpression();
            }

            // Although the spec doesn't capture it we do the same that Dev11 does:
            // Use implicit conversion to Boolean if it is defined on the static type of the left operand.
            // If not the type has to implement IsTrue/IsFalse operator - we checked it during binding.

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var conversion = _compilation.Conversions.ClassifyConversionFromExpression(loweredLeft, boolean, ref useSiteDiagnostics);
            _diagnostics.Add(loweredLeft.Syntax, useSiteDiagnostics);
            if (conversion.IsImplicit)
            {
                Debug.Assert(leftTruthOperator == null);

                var converted = MakeConversionNode(loweredLeft, boolean, @checked: false);
                if (negative)
                {
                    return new BoundUnaryOperator(syntax, UnaryOperatorKind.BoolLogicalNegation, converted, ConstantValue.NotAvailable, MethodSymbol.None, LookupResultKind.Viable, boolean)
                    {
                        WasCompilerGenerated = true
                    };
                }
                else
                {
                    return converted;
                }
            }

            Debug.Assert(leftTruthOperator != null);
            return BoundCall.Synthesized(syntax, null, leftTruthOperator, loweredLeft);
        }

        private BoundExpression LowerUserDefinedBinaryOperator(
            SyntaxNode syntax,
            BinaryOperatorKind operatorKind,
            BoundExpression loweredLeft,
            BoundExpression loweredRight,
            TypeSymbol type,
            MethodSymbol method)
        {
            Debug.Assert(!operatorKind.IsLogical());

            if (operatorKind.IsLifted())
            {
                return RewriteLiftedBinaryOperator(syntax, operatorKind, loweredLeft, loweredRight, type, method);
            }

            // Otherwise, nothing special here.
            Debug.Assert((object)method != null);
            Debug.Assert(TypeSymbol.Equals(method.ReturnType, type, TypeCompareKind.ConsiderEverything2));
            return BoundCall.Synthesized(syntax, null, method, loweredLeft, loweredRight);
        }

        private BoundExpression TrivialLiftedComparisonOperatorOptimizations(
            SyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression left,
            BoundExpression right,
            MethodSymbol method)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);

            // Optimization #1: if both sides are null then the result 
            // is either true (for equality) or false (for everything else.)

            bool leftAlwaysNull = NullableNeverHasValue(left);
            bool rightAlwaysNull = NullableNeverHasValue(right);

            TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);

            if (leftAlwaysNull && rightAlwaysNull)
            {
                return MakeLiteral(syntax, ConstantValue.Create(kind.Operator() == BinaryOperatorKind.Equal), boolType);
            }

            // Optimization #2: If both sides are non-null then we can again eliminate the lifting entirely.

            BoundExpression leftNonNull = NullableAlwaysHasValue(left);
            BoundExpression rightNonNull = NullableAlwaysHasValue(right);

            if (leftNonNull != null && rightNonNull != null)
            {
                return MakeBinaryOperator(
                    syntax: syntax,
                    operatorKind: kind.Unlifted(),
                    loweredLeft: leftNonNull,
                    loweredRight: rightNonNull,
                    type: boolType,
                    method: method);
            }

            // Optimization #3: If one side is null and the other is definitely not, then we generate the side effects
            // of the non-null side and result in true (for not-equals) or false (for everything else.)

            BinaryOperatorKind operatorKind = kind.Operator();

            if (leftAlwaysNull && rightNonNull != null || rightAlwaysNull && leftNonNull != null)
            {
                BoundExpression result = MakeLiteral(syntax, ConstantValue.Create(operatorKind == BinaryOperatorKind.NotEqual), boolType);

                BoundExpression nonNull = leftAlwaysNull ? rightNonNull : leftNonNull;

                if (ReadIsSideeffecting(nonNull))
                {
                    result = new BoundSequence(
                                    syntax: syntax,
                                    locals: ImmutableArray<LocalSymbol>.Empty,
                                    sideEffects: ImmutableArray.Create<BoundExpression>(nonNull),
                                    value: result,
                                    type: boolType);
                }

                return result;
            }

            // Optimization #4: If one side is null and the other is unknown, then we have three cases:
            // #4a: If we have x == null then that becomes !x.HasValue.
            // #4b: If we have x != null then that becomes x.HasValue.
            // #4c: If we have x OP null then that becomes side effects of x, result in false.

            if (leftAlwaysNull || rightAlwaysNull)
            {
                BoundExpression maybeNull = leftAlwaysNull ? right : left;

                if (operatorKind == BinaryOperatorKind.Equal || operatorKind == BinaryOperatorKind.NotEqual)
                {
                    BoundExpression callHasValue = MakeNullableHasValue(syntax, maybeNull);
                    BoundExpression result = operatorKind == BinaryOperatorKind.Equal ?
                        MakeUnaryOperator(UnaryOperatorKind.BoolLogicalNegation, syntax, null, callHasValue, boolType) :
                        callHasValue;
                    return result;
                }
                else
                {
                    BoundExpression falseExpr = MakeBooleanConstant(syntax, operatorKind == BinaryOperatorKind.NotEqual);
                    return _factory.MakeSequence(maybeNull, falseExpr);
                }
            }

            return null;
        }

        private BoundExpression MakeOptimizedGetValueOrDefault(SyntaxNode syntax, BoundExpression expression)
        {
            // If the expression is of nullable type then call GetValueOrDefault. If not,
            // then just use its value.

            if (expression.Type.IsNullableType())
            {
                return BoundCall.Synthesized(syntax, expression, UnsafeGetNullableMethod(syntax, expression.Type, SpecialMember.System_Nullable_T_GetValueOrDefault));
            }

            return expression;
        }

        private BoundExpression MakeBooleanConstant(SyntaxNode syntax, bool value)
        {
            return MakeLiteral(syntax, ConstantValue.Create(value), _compilation.GetSpecialType(SpecialType.System_Boolean));
        }

        private BoundExpression MakeOptimizedHasValue(SyntaxNode syntax, BoundExpression expression)
        {
            // If the expression is of nullable type then call HasValue. If not, then it has a value,
            // so return constant true.
            if (expression.Type.IsNullableType())
            {
                return MakeNullableHasValue(syntax, expression);
            }

            return MakeBooleanConstant(syntax, true);
        }

        private BoundExpression MakeNullableHasValue(SyntaxNode syntax, BoundExpression expression)
        {
            return BoundCall.Synthesized(syntax, expression, UnsafeGetNullableMethod(syntax, expression.Type, SpecialMember.System_Nullable_T_get_HasValue));
        }

        private BoundExpression LowerLiftedBuiltInComparisonOperator(
            SyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression loweredLeft,
            BoundExpression loweredRight)
        {
            // SPEC: For the equality operators == != :
            // SPEC: The lifted operator considers two null values equal and a null value unequal to
            // SPEC: any non-null value. If both operands are non-null the lifted operator unwraps
            // SPEC: the operands and applies the underlying operator to produce the bool result.
            // SPEC:
            // SPEC: For the relational operators < > <= >= :
            // SPEC: The lifted operator produces the value false if one or both operands
            // SPEC: are null. Otherwise the lifted operator unwraps the operands and
            // SPEC: applies the underlying operator to produce the bool result.

            // Note that this means that x == y is true but x <= y is false if both are null.
            // x <= y is not the same as (x < y) || (x == y).

            // Start with some simple optimizations for cases like one side being null.
            BoundExpression optimized = TrivialLiftedComparisonOperatorOptimizations(syntax, kind, loweredLeft, loweredRight, null);
            if (optimized != null)
            {
                return optimized;
            }

            // We rewrite x == y as 
            //
            // tempx = x; 
            // tempy = y;
            // result = (tempx.GetValueOrDefault() == tempy.GetValueOrDefault()) &
            //          (tempx.HasValue == tempy.HasValue);
            //
            // and x != y as
            //
            // tempx = x; 
            // tempy = y;
            // result = !((tempx.GetValueOrDefault() == tempy.GetValueOrDefault()) &
            //            (tempx.HasValue == tempy.HasValue));
            //
            // Otherwise, we rewrite x OP y as
            //
            // tempx = x;
            // tempy = y;
            // result = (tempx.GetValueOrDefault() OP tempy.GetValueOrDefault()) &
            //          (tempx.HasValue & tempy.HasValue);
            //
            //
            // Note that there is no reason to generate "&&" over "&"; the cost of
            // the added code for the conditional branch would be about the same as simply doing 
            // the bitwise & in the first place.
            //
            // We have not yet optimized the case where we have a known-not-null value on one side, 
            // and an unknown value on the other. In those cases we will still generate a temp, but
            // we will not generate the call to the unnecessary nullable ctor or to GetValueOrDefault.
            // Rather, we will generate the value's temp instead of a call to GetValueOrDefault, and generate
            // literal true for HasValue. The tree construction methods we call will use those constants
            // to eliminate unnecessary branches.

            BoundExpression xNonNull = NullableAlwaysHasValue(loweredLeft);
            BoundExpression yNonNull = NullableAlwaysHasValue(loweredRight);

            BoundLocal boundTempX = _factory.StoreToTemp(xNonNull ?? loweredLeft, out BoundAssignmentOperator tempAssignmentX);
            BoundLocal boundTempY = _factory.StoreToTemp(yNonNull ?? loweredRight, out BoundAssignmentOperator tempAssignmentY);

            BoundExpression callX_GetValueOrDefault = MakeOptimizedGetValueOrDefault(syntax, boundTempX);
            BoundExpression callY_GetValueOrDefault = MakeOptimizedGetValueOrDefault(syntax, boundTempY);
            BoundExpression callX_HasValue = MakeOptimizedHasValue(syntax, boundTempX);
            BoundExpression callY_HasValue = MakeOptimizedHasValue(syntax, boundTempY);

            BinaryOperatorKind leftOperator;
            BinaryOperatorKind rightOperator;

            BinaryOperatorKind operatorKind = kind.Operator();
            switch (operatorKind)
            {
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                    leftOperator = BinaryOperatorKind.Equal;
                    rightOperator = BinaryOperatorKind.BoolEqual;
                    break;
                default:
                    leftOperator = operatorKind;
                    rightOperator = BinaryOperatorKind.BoolAnd;
                    break;
            }

            TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);

            // (tempx.GetValueOrDefault() OP tempy.GetValueOrDefault())
            BoundExpression leftExpression = MakeBinaryOperator(
                syntax: syntax,
                operatorKind: leftOperator.WithType(kind.OperandTypes()),
                loweredLeft: callX_GetValueOrDefault,
                loweredRight: callY_GetValueOrDefault,
                type: boolType,
                method: null);

            // (tempx.HasValue OP tempy.HasValue)
            BoundExpression rightExpression = MakeBinaryOperator(
                syntax: syntax,
                operatorKind: rightOperator,
                loweredLeft: callX_HasValue,
                loweredRight: callY_HasValue,
                type: boolType,
                method: null);

            // result = (tempx.GetValueOrDefault() OP tempy.GetValueOrDefault()) &
            //          (tempx.HasValue OP tempy.HasValue)
            BoundExpression binaryExpression = MakeBinaryOperator(
                syntax: syntax,
                operatorKind: BinaryOperatorKind.BoolAnd,
                loweredLeft: leftExpression,
                loweredRight: rightExpression,
                type: boolType,
                method: null);

            // result = !((tempx.GetValueOrDefault() == tempy.GetValueOrDefault()) &
            //            (tempx.HasValue == tempy.HasValue));
            if (operatorKind == BinaryOperatorKind.NotEqual)
            {
                binaryExpression = _factory.Not(binaryExpression);
            }

            // tempx = x; 
            // tempy = y;
            // result = (tempx.GetValueOrDefault() == tempy.GetValueOrDefault()) &
            //          (tempx.HasValue == tempy.HasValue);
            return new BoundSequence(
                syntax: syntax,
                locals: ImmutableArray.Create<LocalSymbol>(boundTempX.LocalSymbol, boundTempY.LocalSymbol),
                sideEffects: ImmutableArray.Create<BoundExpression>(tempAssignmentX, tempAssignmentY),
                value: binaryExpression,
                type: boolType);
        }

        private BoundExpression LowerLiftedUserDefinedComparisonOperator(
            SyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression loweredLeft,
            BoundExpression loweredRight,
            MethodSymbol method)
        {
            // If both sides are null, or neither side is null, then we can do some simple optimizations.

            BoundExpression optimized = TrivialLiftedComparisonOperatorOptimizations(syntax, kind, loweredLeft, loweredRight, method);
            if (optimized != null)
            {
                return optimized;
            }

            // Otherwise, the expression
            //
            // x == y 
            //
            // becomes
            //
            // tempX = x;
            // tempY = y;
            // result = tempX.HasValue == tempY.HasValue ? 
            //            (tempX.HasValue ? 
            //              tempX.GetValueOrDefault() == tempY.GetValueOrDefault() : 
            //              true) : 
            //          false;
            //
            //
            // the expression
            //
            // x != y 
            //
            // becomes
            //
            // tempX = x;
            // tempY = y;
            // result = tempX.HasValue == tempY.HasValue ? 
            //            (tempX.HasValue ? 
            //              tempX.GetValueOrDefault() != tempY.GetValueOrDefault() : 
            //              false) : 
            //            true;
            //
            //
            // For the other comparison operators <, <=, >, >=,
            //
            // x OP y 
            //
            // becomes
            //
            // tempX = x;
            // tempY = y;
            // result = tempX.HasValue & tempY.HasValue ? 
            //              tempX.GetValueOrDefault() OP tempY.GetValueOrDefault() : 
            //              false;
            //
            // We have not yet optimized the case where we have a known-not-null value on one side, 
            // and an unknown value on the other. In those cases we will still generate a temp, but
            // we will not generate the call to the unnecessary nullable ctor or to GetValueOrDefault.
            // Rather, we will generate the value's temp instead of a call to GetValueOrDefault, and generate
            // literal true for HasValue. The tree construction methods we call will use those constants
            // to eliminate unnecessary branches.

            BoundExpression xNonNull = NullableAlwaysHasValue(loweredLeft);
            BoundExpression yNonNull = NullableAlwaysHasValue(loweredRight);

            // TODO: (This TODO applies throughout this file, not just to this method.)
            // TODO: We might be storing a constant to this temporary that we could simply inline.
            // TODO: (There are other expressions that can be safely moved around other than constants
            // TODO: as well -- for example a boxing conversion of a constant int to object.)
            // TODO: Build a better temporary-storage management system that decides whether or not
            // TODO: to store a temporary.

            BoundAssignmentOperator tempAssignmentX;
            BoundLocal boundTempX = _factory.StoreToTemp(xNonNull ?? loweredLeft, out tempAssignmentX);
            BoundAssignmentOperator tempAssignmentY;
            BoundLocal boundTempY = _factory.StoreToTemp(yNonNull ?? loweredRight, out tempAssignmentY);

            BoundExpression callX_GetValueOrDefault = MakeOptimizedGetValueOrDefault(syntax, boundTempX);
            BoundExpression callY_GetValueOrDefault = MakeOptimizedGetValueOrDefault(syntax, boundTempY);
            BoundExpression callX_HasValue = MakeOptimizedHasValue(syntax, boundTempX);
            BoundExpression callY_HasValue = MakeOptimizedHasValue(syntax, boundTempY);

            // tempx.HasValue == tempy.HasValue
            BinaryOperatorKind conditionOperator;
            BinaryOperatorKind operatorKind = kind.Operator();
            switch (operatorKind)
            {
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                    conditionOperator = BinaryOperatorKind.BoolEqual;
                    break;
                default:
                    conditionOperator = BinaryOperatorKind.BoolAnd;
                    break;
            }

            TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);

            BoundExpression condition = MakeBinaryOperator(
                syntax: syntax,
                operatorKind: conditionOperator,
                loweredLeft: callX_HasValue,
                loweredRight: callY_HasValue,
                type: boolType,
                method: null);

            // tempX.GetValueOrDefault() OP tempY.GetValueOrDefault()
            BoundExpression unliftedOp = MakeBinaryOperator(
                syntax: syntax,
                operatorKind: kind.Unlifted(),
                loweredLeft: callX_GetValueOrDefault,
                loweredRight: callY_GetValueOrDefault,
                type: boolType,
                method: method);

            BoundExpression consequence;
            if (operatorKind == BinaryOperatorKind.Equal || operatorKind == BinaryOperatorKind.NotEqual)
            {
                // tempx.HasValue ? tempX.GetValueOrDefault() == tempY.GetValueOrDefault() : true
                consequence = RewriteConditionalOperator(
                    syntax: syntax,
                    rewrittenCondition: callX_HasValue,
                    rewrittenConsequence: unliftedOp,
                    rewrittenAlternative: MakeLiteral(syntax, ConstantValue.Create(operatorKind == BinaryOperatorKind.Equal), boolType),
                    constantValueOpt: null,
                    rewrittenType: boolType,
                    isRef: false);
            }
            else
            {
                // tempX.GetValueOrDefault() OP tempY.GetValueOrDefault()
                consequence = unliftedOp;
            }

            // false
            BoundExpression alternative = MakeBooleanConstant(syntax, operatorKind == BinaryOperatorKind.NotEqual);

            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: syntax,
                rewrittenCondition: condition,
                rewrittenConsequence: consequence,
                rewrittenAlternative: alternative,
                constantValueOpt: null,
                rewrittenType: boolType,
                isRef: false);

            return new BoundSequence(
                syntax: syntax,
                locals: ImmutableArray.Create<LocalSymbol>(boundTempX.LocalSymbol, boundTempY.LocalSymbol),
                sideEffects: ImmutableArray.Create<BoundExpression>(tempAssignmentX, tempAssignmentY),
                value: conditionalExpression,
                type: boolType);
        }

        private BoundExpression TrivialLiftedBinaryArithmeticOptimizations(
            SyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression left,
            BoundExpression right,
            TypeSymbol type,
            MethodSymbol method)
        {
            // We begin with a trivial optimization: if both operands are null then the 
            // result is known to be null.

            Debug.Assert(left != null);
            Debug.Assert(right != null);

            // Optimization #1: if both sides are null then the result 
            // is either true (for equality) or false (for everything else.)

            bool leftAlwaysNull = NullableNeverHasValue(left);
            bool rightAlwaysNull = NullableNeverHasValue(right);

            if (leftAlwaysNull && rightAlwaysNull)
            {
                // default(R?)
                return new BoundDefaultExpression(syntax, type);
            }

            // Optimization #2: If both sides are non-null then we can again eliminate the lifting entirely.

            BoundExpression leftNonNull = NullableAlwaysHasValue(left);
            BoundExpression rightNonNull = NullableAlwaysHasValue(right);

            if (leftNonNull != null && rightNonNull != null)
            {
                return MakeLiftedBinaryOperatorConsequence(syntax, kind, leftNonNull, rightNonNull, type, method);
            }

            return null;
        }

        private BoundExpression MakeLiftedBinaryOperatorConsequence(
            SyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression left,
            BoundExpression right,
            TypeSymbol type,
            MethodSymbol method)
        {
            // tempX.GetValueOrDefault() OP tempY.GetValueOrDefault()
            BoundExpression unliftedOp = MakeBinaryOperator(
                syntax: syntax,
                operatorKind: kind.Unlifted(),
                loweredLeft: left,
                loweredRight: right,
                type: type.GetNullableUnderlyingType(),
                method: method);

            // new R?(tempX.GetValueOrDefault() OP tempY.GetValueOrDefault)
            return new BoundObjectCreationExpression(
                syntax,
                UnsafeGetNullableMethod(syntax, type, SpecialMember.System_Nullable_T__ctor),
                null,
                unliftedOp);
        }

        private static BoundExpression OptimizeLiftedArithmeticOperatorOneNull(
            SyntaxNode syntax,
            BoundExpression left,
            BoundExpression right,
            TypeSymbol type)
        {
            // Here we optimize the cases where one side is known to be null. If we have
            // null + M() or null + new int?(M()) then we simply generate M() as a side 
            // effect and produce null. Note that we can optimize away the unnecessary
            // constructor.

            bool leftAlwaysNull = NullableNeverHasValue(left);
            bool rightAlwaysNull = NullableNeverHasValue(right);

            Debug.Assert(!(leftAlwaysNull && rightAlwaysNull)); // We've already optimized this case.

            if (!(leftAlwaysNull || rightAlwaysNull))
            {
                return null;
            }

            BoundExpression notAlwaysNull = leftAlwaysNull ? right : left;
            BoundExpression neverNull = NullableAlwaysHasValue(notAlwaysNull);
            BoundExpression sideEffect = neverNull ?? notAlwaysNull;

            // If the "side effect" is a constant then we simply elide it entirely.

            // TODO: There are expressions other than constants that have no side effects
            // TODO: or elidable side effects.

            if (sideEffect.ConstantValue != null)
            {
                return new BoundDefaultExpression(syntax, type);
            }

            return new BoundSequence(
                syntax: syntax,
                locals: ImmutableArray<LocalSymbol>.Empty,
                sideEffects: ImmutableArray.Create<BoundExpression>(sideEffect),
                value: new BoundDefaultExpression(syntax, type),
                type: type);
        }

        private BoundExpression LowerLiftedBinaryArithmeticOperator(
            SyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression loweredLeft,
            BoundExpression loweredRight,
            TypeSymbol type,
            MethodSymbol method)
        {
            // We have a lifted * / % + - << >> ^ & | binary operator. We begin with trivial
            // optimizations; if both sides are null or neither side is null then we can
            // eliminate the lifting altogether.

            BoundExpression optimized = OptimizeLiftedBinaryArithmetic(syntax, kind, loweredLeft, loweredRight, type, method);
            if (optimized != null)
            {
                return optimized;
            }

            // We now know that neither side is null. However, we might have an operand that is known
            // to be non-null. If neither side is known to be non-null then we generate:
            //
            // S? tempX = left;
            // S? tempY = right;
            // R? r = tempX.HasValue & tempY.HasValue ? 
            //        new R?(tempX.GetValueOrDefault() OP tempY.GetValueOrDefault())  :
            //        default(R?);
            //
            // If one of the operands, say the right, is non-null, then we generate:
            //
            // S? tempX = left;
            // S tempY = right; // not null
            // R? r = tempX.HasValue  ? 
            //        new R?(tempX.GetValueOrDefault() OP tempY)  :
            //        default(R?);
            //

            var sideeffects = ArrayBuilder<BoundExpression>.GetInstance();
            var locals = ArrayBuilder<LocalSymbol>.GetInstance();

            BoundExpression leftNeverNull = NullableAlwaysHasValue(loweredLeft);
            BoundExpression rightNeverNull = NullableAlwaysHasValue(loweredRight);

            BoundExpression boundTempX = leftNeverNull ?? loweredLeft;
            boundTempX = CaptureExpressionInTempIfNeeded(boundTempX, sideeffects, locals);

            BoundExpression boundTempY = rightNeverNull ?? loweredRight;
            boundTempY = CaptureExpressionInTempIfNeeded(boundTempY, sideeffects, locals);

            BoundExpression callX_GetValueOrDefault = MakeOptimizedGetValueOrDefault(syntax, boundTempX);
            BoundExpression callY_GetValueOrDefault = MakeOptimizedGetValueOrDefault(syntax, boundTempY);
            BoundExpression callX_HasValue = MakeOptimizedHasValue(syntax, boundTempX);
            BoundExpression callY_HasValue = MakeOptimizedHasValue(syntax, boundTempY);

            // tempX.HasValue & tempY.HasValue
            TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);
            BoundExpression condition = MakeBinaryOperator(syntax, BinaryOperatorKind.BoolAnd, callX_HasValue, callY_HasValue, boolType, null);

            // new R?(tempX.GetValueOrDefault() OP tempY.GetValueOrDefault)
            BoundExpression consequence = MakeLiftedBinaryOperatorConsequence(syntax, kind, callX_GetValueOrDefault, callY_GetValueOrDefault, type, method);

            // default(R?)
            BoundExpression alternative = new BoundDefaultExpression(syntax, type);

            // tempX.HasValue & tempY.HasValue ? 
            //          new R?(tempX.GetValueOrDefault() OP tempY.GetValueOrDefault()) : 
            //          default(R?);
            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: syntax,
                rewrittenCondition: condition,
                rewrittenConsequence: consequence,
                rewrittenAlternative: alternative,
                constantValueOpt: null,
                rewrittenType: type,
                isRef: false);

            return new BoundSequence(
                syntax: syntax,
                locals: locals.ToImmutableAndFree(),
                sideEffects: sideeffects.ToImmutableAndFree(),
                value: conditionalExpression,
                type: type);
        }

        private BoundExpression CaptureExpressionInTempIfNeeded(
            BoundExpression operand,
            ArrayBuilder<BoundExpression> sideeffects,
            ArrayBuilder<LocalSymbol> locals,
            SynthesizedLocalKind kind = SynthesizedLocalKind.LoweringTemp)
        {
            if (CanChangeValueBetweenReads(operand))
            {
                BoundAssignmentOperator tempAssignment;
                var tempAccess = _factory.StoreToTemp(operand, out tempAssignment, kind: kind);
                sideeffects.Add(tempAssignment);
                locals.Add(tempAccess.LocalSymbol);
                operand = tempAccess;
            }

            return operand;
        }

        private BoundExpression OptimizeLiftedBinaryArithmetic(
            SyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression left,
            BoundExpression right,
            TypeSymbol type,
            MethodSymbol method)
        {
            BoundExpression optimized = TrivialLiftedBinaryArithmeticOptimizations(syntax, kind, left, right, type, method);
            if (optimized != null)
            {
                return optimized;
            }

            // Boolean & and | operators have completely different codegen in non-trivial cases.
            if (kind == BinaryOperatorKind.LiftedBoolAnd || kind == BinaryOperatorKind.LiftedBoolOr)
            {
                return LowerLiftedBooleanOperator(syntax, kind, left, right);
            }

            // If one side is null then we can lower this to a side effect and a null value.
            optimized = OptimizeLiftedArithmeticOperatorOneNull(syntax, left, right, type);
            if (optimized != null)
            {
                return optimized;
            }

            // This is a bit of a complicated optimization; it is a more complex version of the 
            // "distributed" optimizations for lifted conversions and lifted unary operators.
            //
            // Suppose we have a lifted binary operation where the left side might be null or not,
            // and the right side is definitely not, and moreover, it is a constant.  We are particularly
            // concerned about this because expressions like "i++" and "i += 1" are lowered to "i = i + 1";
            // it is quite common for there to be a constant on the left hand side of a lifted binop.
            //
            // Here N() returns int?:
            //
            // return N() + 1;
            //
            // In LowerLiftedBinaryOperator, above, we would optimize this as:
            //
            // int? n = N();
            // int v = 1;
            // return n.HasValue ? new int?(n.Value + v) : new int?()
            //
            // This is unfortunate in that we generate an unnecessary temporary, but that's
            // not what we're going to optimize away here. This codegen is pretty good. 
            //
            // Now let's suppose that instead of N(), we have a lifted operation on the left side:
            //
            // return (N1() * N2()) + 1;
            //
            // We could realize the left hand term as a lifted multiplication, produce a nullable int,
            // assign it to a temporary, and do the lifted addition:
            //
            // int? n1 = N1();
            // int? n2 = N2();
            // int? r = n1.HasValue & n2.HasValue ? new int?(n1.Value * n2.Value) : new int?();
            // int v = 1;
            // return r.HasValue ? new int?(r.Value + v) : new int?();
            //
            // But what is the point of the temporary r in this expansion?  We can eliminate it, and 
            // thereby eliminate two int? constructors in the process.  We can realize this as:
            //
            // int? n1 = N1();
            // int? n2 = N2();
            // return n1.HasValue & n2.HasValue ? new int?(n1.Value * n2.Value + 1) : new int?();
            //
            // We eliminate both the temporaries r and v.
            //
            // Now, a reasonable question at this point would be "well, suppose the expression on
            // the right side is not a constant, but is known to be non-null; can we do the same
            // optimization?"  No, and here's why. Suppose we have:
            //
            // return (N1() * N2()) + V();
            //
            // We cannot realize this as:
            //
            // int? n1 = N1();
            // int? n2 = N2();
            // int v = V();
            // return n1.HasValue & n2.HasValue ? new int?(n1.Value * n2.Value + v) : new int?();
            //
            // because this changes the order in which the operations happen. Before we had N1(),
            // N2(), the multiplication -- which might be a user-defined operator with a side effect,
            // or might be in a checked context and overflow -- and then V().  Now we have V() before
            // the multiplication, changing the order in which side effects occur.
            //
            // We could solve this problem by generating:
            //
            // int? n1 = N1();
            // int? n2 = N2();
            // return n1.HasValue & n2.HasValue ? new int?(n1.Value * n2.Value + V()) : (V(), new int?());

            // so that the side effects of V() happen after the multiplication or before the value is 
            // produced, but now we have generated IL for V() twice. That could be a complex expression
            // and the whole point of this optimization is to make less IL. 
            //
            // We could, however, optimize it if the non-null V() was on the left hand side. At this
            // time we will not pursue that optimization. We're really just hoping to get the N1() * N2() + 1
            // operation smaller; the rest is gravy.

            BoundExpression nonNullRight = NullableAlwaysHasValue(right);
            if (nonNullRight != null && nonNullRight.ConstantValue != null && left.Kind == BoundKind.Sequence)
            {
                BoundSequence seq = (BoundSequence)left;
                if (seq.Value.Kind == BoundKind.ConditionalOperator)
                {
                    BoundConditionalOperator conditional = (BoundConditionalOperator)seq.Value;
                    Debug.Assert(TypeSymbol.Equals(seq.Type, conditional.Type, TypeCompareKind.ConsiderEverything2));
                    Debug.Assert(TypeSymbol.Equals(conditional.Type, conditional.Consequence.Type, TypeCompareKind.ConsiderEverything2));
                    Debug.Assert(TypeSymbol.Equals(conditional.Type, conditional.Alternative.Type, TypeCompareKind.ConsiderEverything2));

                    if (NullableAlwaysHasValue(conditional.Consequence) != null && NullableNeverHasValue(conditional.Alternative))
                    {
                        return new BoundSequence(
                            syntax,
                            seq.Locals,
                            seq.SideEffects,
                            RewriteConditionalOperator(
                                syntax,
                                conditional.Condition,
                                MakeBinaryOperator(syntax, kind, conditional.Consequence, right, type, method),
                                MakeBinaryOperator(syntax, kind, conditional.Alternative, right, type, method),
                                ConstantValue.NotAvailable,
                                type,
                                isRef: false),
                            type);
                    }
                }
            }

            return null;
        }

        private BoundExpression MakeNewNullableBoolean(SyntaxNode syntax, bool? value)
        {
            NamedTypeSymbol nullableType = _compilation.GetSpecialType(SpecialType.System_Nullable_T);
            TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);
            NamedTypeSymbol nullableBoolType = nullableType.Construct(boolType);
            if (value == null)
            {
                return new BoundDefaultExpression(syntax, nullableBoolType);
            }

            return new BoundObjectCreationExpression(
                syntax,
                UnsafeGetNullableMethod(syntax, nullableBoolType, SpecialMember.System_Nullable_T__ctor),
                null,
                MakeBooleanConstant(syntax, value.GetValueOrDefault()));
        }

        private BoundExpression OptimizeLiftedBooleanOperatorOneNull(
            SyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression left,
            BoundExpression right)
        {
            // Here we optimize the cases where one side is known to be null.

            bool leftAlwaysNull = NullableNeverHasValue(left);
            bool rightAlwaysNull = NullableNeverHasValue(right);

            Debug.Assert(!(leftAlwaysNull && rightAlwaysNull)); // We've already optimized this case.

            if (!(leftAlwaysNull || rightAlwaysNull))
            {
                return null;
            }

            // First, if one operand is null and the other is definitely non null, then we can eliminate
            // all the temporaries:
            //
            // new bool?() & new bool?(B())
            // new bool?() | new bool?(B())
            //
            // can be generated as
            //
            // B() ? new bool?() : new bool?(false)
            // B() ? new bool?(true) : new bool?()
            //
            // respectively.

            BoundExpression alwaysNull = leftAlwaysNull ? left : right;
            BoundExpression notAlwaysNull = leftAlwaysNull ? right : left;
            BoundExpression neverNull = NullableAlwaysHasValue(notAlwaysNull);
            BoundExpression nullBool = new BoundDefaultExpression(syntax, alwaysNull.Type);

            if (neverNull != null)
            {
                BoundExpression newNullBool = MakeNewNullableBoolean(syntax, kind == BinaryOperatorKind.LiftedBoolOr);

                return RewriteConditionalOperator(
                    syntax: syntax,
                    rewrittenCondition: neverNull,
                    rewrittenConsequence: kind == BinaryOperatorKind.LiftedBoolAnd ? nullBool : newNullBool,
                    rewrittenAlternative: kind == BinaryOperatorKind.LiftedBoolAnd ? newNullBool : nullBool,
                    constantValueOpt: null,
                    rewrittenType: alwaysNull.Type,
                    isRef: false);
            }

            // Now we optimize the case where one operand is null and the other is not. We generate
            //
            // new bool?() & M() 
            // new bool?() | M()
            //
            // as 
            //
            // bool? t = M(), t.GetValueOrDefault() ? new bool?() : t
            // bool? t = M(), t.GetValueOrDefault() ? t : new bool?()
            //
            // respectively.

            BoundAssignmentOperator tempAssignment;
            BoundLocal boundTemp = _factory.StoreToTemp(notAlwaysNull, out tempAssignment);
            BoundExpression condition = MakeOptimizedGetValueOrDefault(syntax, boundTemp);
            BoundExpression consequence = kind == BinaryOperatorKind.LiftedBoolAnd ? nullBool : boundTemp;
            BoundExpression alternative = kind == BinaryOperatorKind.LiftedBoolAnd ? boundTemp : nullBool;
            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: syntax,
                rewrittenCondition: condition,
                rewrittenConsequence: consequence,
                rewrittenAlternative: alternative,
                constantValueOpt: null,
                rewrittenType: alwaysNull.Type,
                isRef: false);
            return new BoundSequence(
                syntax: syntax,
                locals: ImmutableArray.Create<LocalSymbol>(boundTemp.LocalSymbol),
                sideEffects: ImmutableArray.Create<BoundExpression>(tempAssignment),
                value: conditionalExpression,
                type: conditionalExpression.Type);
        }

        private BoundExpression OptimizeLiftedBooleanOperatorOneNonNull(
            SyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression left,
            BoundExpression right)
        {
            // Here we optimize the cases where one side is known to be non-null.  We generate:
            //
            // new bool?(B()) & N()
            // N() & new bool?(B())
            // new bool?(B()) | N()
            // N() | new bool?(B())
            //
            // as
            //
            // bool b = B(), bool? n = N(), b ? n : new bool?(false)
            // bool? n = N(), bool b = B(), b ? n : new bool?(false)
            // bool b = B(), bool? n = N(), b ? new bool?(true) : n
            // bool? n = N(), bool b = B(), b ? new bool?(true) : n
            //
            // respectively.

            BoundExpression leftNonNull = NullableAlwaysHasValue(left);
            BoundExpression rightNonNull = NullableAlwaysHasValue(right);

            Debug.Assert(leftNonNull == null || rightNonNull == null); // We've already optimized the case where they are both non-null.
            Debug.Assert(!NullableNeverHasValue(left) && !NullableNeverHasValue(right)); // We've already optimized the case where one is null.

            if (leftNonNull == null && rightNonNull == null)
            {
                return null;
            }

            // One is definitely not null and the other might be null.

            BoundAssignmentOperator tempAssignmentX;
            BoundLocal boundTempX = _factory.StoreToTemp(leftNonNull ?? left, out tempAssignmentX);
            BoundAssignmentOperator tempAssignmentY;
            BoundLocal boundTempY = _factory.StoreToTemp(rightNonNull ?? right, out tempAssignmentY);
            BoundExpression nonNullTemp = leftNonNull == null ? boundTempY : boundTempX;
            BoundExpression maybeNullTemp = leftNonNull == null ? boundTempX : boundTempY;
            BoundExpression condition = nonNullTemp;
            BoundExpression newNullBool = MakeNewNullableBoolean(syntax, kind == BinaryOperatorKind.LiftedBoolOr);
            BoundExpression consequence = kind == BinaryOperatorKind.LiftedBoolOr ? newNullBool : maybeNullTemp;
            BoundExpression alternative = kind == BinaryOperatorKind.LiftedBoolOr ? maybeNullTemp : newNullBool;
            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: syntax,
                rewrittenCondition: condition,
                rewrittenConsequence: consequence,
                rewrittenAlternative: alternative,
                constantValueOpt: null,
                rewrittenType: newNullBool.Type,
                isRef: false);
            return new BoundSequence(
                syntax: syntax,
                locals: ImmutableArray.Create<LocalSymbol>(boundTempX.LocalSymbol, boundTempY.LocalSymbol),
                sideEffects: ImmutableArray.Create<BoundExpression>(tempAssignmentX, tempAssignmentY),
                value: conditionalExpression,
                type: conditionalExpression.Type);
        }

        private BoundExpression LowerLiftedBooleanOperator(
            SyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression loweredLeft,
            BoundExpression loweredRight)
        {
            // x & y and x | y have special codegen if x and y are nullable Booleans.

            // We have already optimized cases where both operands are null or both are non-null.
            // Now optimize cases where one side is known to be null or one side is known to be non-null.
            BoundExpression optimized = OptimizeLiftedBooleanOperatorOneNull(syntax, kind, loweredLeft, loweredRight);
            if (optimized != null)
            {
                return optimized;
            }

            optimized = OptimizeLiftedBooleanOperatorOneNonNull(syntax, kind, loweredLeft, loweredRight);
            if (optimized != null)
            {
                return optimized;
            }

            // x & y is realized as (x.GetValueOrDefault() || !(y.GetValueOrDefault() || x.HasValue)) ? y : x
            // x | y is realized as (x.GetValueOrDefault() || !(y.GetValueOrDefault() || x.HasValue)) ? x : y

            // CONSIDER: Consider realizing these using | instead of ||. 
            // CONSIDER: The operations are extremely low cost and the added bulk to the code might not be worthwhile.

            BoundAssignmentOperator tempAssignmentX;
            BoundLocal boundTempX = _factory.StoreToTemp(loweredLeft, out tempAssignmentX);
            BoundAssignmentOperator tempAssignmentY;
            BoundLocal boundTempY = _factory.StoreToTemp(loweredRight, out tempAssignmentY);

            TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);

            MethodSymbol getValueOrDefaultX = UnsafeGetNullableMethod(syntax, boundTempX.Type, SpecialMember.System_Nullable_T_GetValueOrDefault);
            MethodSymbol getValueOrDefaultY = UnsafeGetNullableMethod(syntax, boundTempY.Type, SpecialMember.System_Nullable_T_GetValueOrDefault);

            // tempx.GetValueOrDefault()
            BoundExpression callX_GetValueOrDefault = BoundCall.Synthesized(syntax, boundTempX, getValueOrDefaultX);
            // tempy.GetValueOrDefault()
            BoundExpression callY_GetValueOrDefault = BoundCall.Synthesized(syntax, boundTempY, getValueOrDefaultY);
            // tempx.HasValue
            BoundExpression callX_HasValue = MakeNullableHasValue(syntax, boundTempX);

            // (tempy.GetValueOrDefault || tempx.HasValue)
            BoundExpression innerOr = MakeBinaryOperator(
                syntax: syntax,
                operatorKind: BinaryOperatorKind.LogicalBoolOr,
                loweredLeft: callY_GetValueOrDefault,
                loweredRight: callX_HasValue,
                type: boolType,
                method: null);

            // !(tempy.GetValueOrDefault || tempx.HasValue)
            BoundExpression invert = MakeUnaryOperator(UnaryOperatorKind.BoolLogicalNegation, syntax, null, innerOr, boolType);

            // (x.GetValueOrDefault() || !(y.GetValueOrDefault() || x.HasValue))
            BoundExpression condition = MakeBinaryOperator(
                syntax: syntax,
                operatorKind: BinaryOperatorKind.LogicalBoolOr,
                loweredLeft: callX_GetValueOrDefault,
                loweredRight: invert,
                type: boolType,
                method: null);

            BoundExpression consequence = kind == BinaryOperatorKind.LiftedBoolAnd ? boundTempY : boundTempX;
            BoundExpression alternative = kind == BinaryOperatorKind.LiftedBoolAnd ? boundTempX : boundTempY;

            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: syntax,
                rewrittenCondition: condition,
                rewrittenConsequence: consequence,
                rewrittenAlternative: alternative,
                constantValueOpt: null,
                rewrittenType: alternative.Type,
                isRef: false);

            return new BoundSequence(
                syntax: syntax,
                locals: ImmutableArray.Create<LocalSymbol>(boundTempX.LocalSymbol, boundTempY.LocalSymbol),
                sideEffects: ImmutableArray.Create<BoundExpression>(tempAssignmentX, tempAssignmentY),
                value: conditionalExpression,
                type: conditionalExpression.Type);
        }

        /// <summary>
        /// This function provides a false sense of security, it is likely going to surprise you when the requested member is missing.
        /// Recommendation: Do not use, use <see cref="TryGetNullableMethod"/> instead! 
        /// If used, a unit-test with a missing member is absolutely a must have.
        /// </summary>
        private MethodSymbol UnsafeGetNullableMethod(SyntaxNode syntax, TypeSymbol nullableType, SpecialMember member)
        {
            return UnsafeGetNullableMethod(syntax, nullableType, member, _compilation, _diagnostics);
        }

        /// <summary>
        /// This function provides a false sense of security, it is likely going to surprise you when the requested member is missing.
        /// Recommendation: Do not use, use <see cref="TryGetNullableMethod"/> instead! 
        /// If used, a unit-test with a missing member is absolutely a must have.
        /// </summary>
        private static MethodSymbol UnsafeGetNullableMethod(SyntaxNode syntax, TypeSymbol nullableType, SpecialMember member, CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            var nullableType2 = nullableType as NamedTypeSymbol;
            Debug.Assert((object)nullableType2 != null);
            return UnsafeGetSpecialTypeMethod(syntax, member, compilation, diagnostics).AsMember(nullableType2);
        }

        private bool TryGetNullableMethod(SyntaxNode syntax, TypeSymbol nullableType, SpecialMember member, out MethodSymbol result)
        {
            var nullableType2 = (NamedTypeSymbol)nullableType;
            if (TryGetSpecialTypeMethod(syntax, member, out result))
            {
                result = result.AsMember(nullableType2);
                return true;
            }

            return false;
        }

        private BoundExpression RewriteNullableNullEquality(
            SyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression loweredLeft,
            BoundExpression loweredRight,
            TypeSymbol returnType)
        {
            // This handles the case where we have a nullable user-defined struct type compared against null, eg:
            //
            // struct S {} ... S? s = whatever; if (s != null)
            //
            // If S does not define an overloaded != operator then this is lowered to s.HasValue.
            //
            // If the type already has a user-defined or built-in operator then comparing to null is
            // treated as a lifted equality operator.

            Debug.Assert(loweredLeft != null);
            Debug.Assert(loweredRight != null);
            Debug.Assert((object)returnType != null);
            Debug.Assert(returnType.SpecialType == SpecialType.System_Boolean);
            Debug.Assert(loweredLeft.IsLiteralNull() != loweredRight.IsLiteralNull());

            BoundExpression nullable = loweredRight.IsLiteralNull() ? loweredLeft : loweredRight;

            // If the other side is known to always be null then we can simply generate true or false, as appropriate.

            if (NullableNeverHasValue(nullable))
            {
                return MakeLiteral(syntax, ConstantValue.Create(kind == BinaryOperatorKind.NullableNullEqual), returnType);
            }

            BoundExpression nonNullValue = NullableAlwaysHasValue(nullable);
            if (nonNullValue != null)
            {
                // We have something like "if (new int?(M()) != null)". We can optimize this to
                // evaluate M() for its side effects and then result in true or false, as appropriate.

                // TODO: If the expression has no side effects then it can be optimized away here as well.

                return new BoundSequence(
                    syntax: syntax,
                    locals: ImmutableArray<LocalSymbol>.Empty,
                    sideEffects: ImmutableArray.Create<BoundExpression>(nonNullValue),
                    value: MakeBooleanConstant(syntax, kind == BinaryOperatorKind.NullableNullNotEqual),
                    type: returnType);
            }

            // arr?.Length == null
            var conditionalAccess = nullable as BoundLoweredConditionalAccess;
            if (conditionalAccess != null &&
                (conditionalAccess.WhenNullOpt == null || conditionalAccess.WhenNullOpt.IsDefaultValue()))
            {
                BoundExpression whenNotNull = RewriteNullableNullEquality(
                    syntax,
                    kind,
                    conditionalAccess.WhenNotNull,
                    loweredLeft.IsLiteralNull() ? loweredLeft : loweredRight,
                    returnType);

                var whenNull = kind == BinaryOperatorKind.NullableNullEqual ? MakeBooleanConstant(syntax, true) : null;

                return conditionalAccess.Update(conditionalAccess.Receiver, conditionalAccess.HasValueMethodOpt, whenNotNull, whenNull, conditionalAccess.Id, whenNotNull.Type);
            }

            BoundExpression call = MakeNullableHasValue(syntax, nullable);
            BoundExpression result = kind == BinaryOperatorKind.NullableNullNotEqual ?
                call :
                new BoundUnaryOperator(syntax, UnaryOperatorKind.BoolLogicalNegation, call, ConstantValue.NotAvailable, null, LookupResultKind.Viable, returnType);

            return result;
        }

        private BoundExpression RewriteStringEquality(BoundBinaryOperator oldNode, SyntaxNode syntax, BinaryOperatorKind operatorKind, BoundExpression loweredLeft, BoundExpression loweredRight, TypeSymbol type, SpecialMember member)
        {
            if (oldNode != null && (loweredLeft.ConstantValue == ConstantValue.Null || loweredRight.ConstantValue == ConstantValue.Null))
            {
                return oldNode.Update(operatorKind, oldNode.ConstantValueOpt, oldNode.MethodOpt, oldNode.ResultKind, loweredLeft, loweredRight, type);
            }

            var method = UnsafeGetSpecialTypeMethod(syntax, member);
            Debug.Assert((object)method != null);

            return BoundCall.Synthesized(syntax, null, method, loweredLeft, loweredRight);
        }

        private BoundExpression RewriteDelegateOperation(SyntaxNode syntax, BinaryOperatorKind operatorKind, BoundExpression loweredLeft, BoundExpression loweredRight, TypeSymbol type, SpecialMember member)
        {
            MethodSymbol method;
            if (operatorKind == BinaryOperatorKind.DelegateEqual || operatorKind == BinaryOperatorKind.DelegateNotEqual)
            {
                method = (MethodSymbol)_compilation.Assembly.GetSpecialTypeMember(member);
                if (loweredRight.IsLiteralNull() ||
                    loweredLeft.IsLiteralNull() ||
                    (object)(method = (MethodSymbol)_compilation.Assembly.GetSpecialTypeMember(member)) == null)
                {
                    // use reference equality in the absence of overloaded operators for System.Delegate.
                    operatorKind = (operatorKind & (~BinaryOperatorKind.Delegate)) | BinaryOperatorKind.Object;
                    return new BoundBinaryOperator(syntax, operatorKind, default(ConstantValue), null, LookupResultKind.Empty, loweredLeft, loweredRight, type);
                }
            }
            else
            {
                method = UnsafeGetSpecialTypeMethod(syntax, member);
            }

            Debug.Assert((object)method != null);
            BoundExpression call = _inExpressionLambda
                ? new BoundBinaryOperator(syntax, operatorKind, null, method, default(LookupResultKind), loweredLeft, loweredRight, method.ReturnType)
                : (BoundExpression)BoundCall.Synthesized(syntax, null, method, loweredLeft, loweredRight);
            BoundExpression result = method.ReturnType.SpecialType == SpecialType.System_Delegate ?
                MakeConversionNode(syntax, call, Conversion.ExplicitReference, type, @checked: false) :
                call;
            return result;
        }

        private BoundExpression RewriteDecimalBinaryOperation(SyntaxNode syntax, BoundExpression loweredLeft, BoundExpression loweredRight, BinaryOperatorKind operatorKind)
        {
            Debug.Assert(loweredLeft.Type.SpecialType == SpecialType.System_Decimal);
            Debug.Assert(loweredRight.Type.SpecialType == SpecialType.System_Decimal);

            SpecialMember member;

            switch (operatorKind)
            {
                case BinaryOperatorKind.DecimalAddition: member = SpecialMember.System_Decimal__op_Addition; break;
                case BinaryOperatorKind.DecimalSubtraction: member = SpecialMember.System_Decimal__op_Subtraction; break;
                case BinaryOperatorKind.DecimalMultiplication: member = SpecialMember.System_Decimal__op_Multiply; break;
                case BinaryOperatorKind.DecimalDivision: member = SpecialMember.System_Decimal__op_Division; break;
                case BinaryOperatorKind.DecimalRemainder: member = SpecialMember.System_Decimal__op_Modulus; break;
                case BinaryOperatorKind.DecimalEqual: member = SpecialMember.System_Decimal__op_Equality; break;
                case BinaryOperatorKind.DecimalNotEqual: member = SpecialMember.System_Decimal__op_Inequality; break;
                case BinaryOperatorKind.DecimalLessThan: member = SpecialMember.System_Decimal__op_LessThan; break;
                case BinaryOperatorKind.DecimalLessThanOrEqual: member = SpecialMember.System_Decimal__op_LessThanOrEqual; break;
                case BinaryOperatorKind.DecimalGreaterThan: member = SpecialMember.System_Decimal__op_GreaterThan; break;
                case BinaryOperatorKind.DecimalGreaterThanOrEqual: member = SpecialMember.System_Decimal__op_GreaterThanOrEqual; break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(operatorKind);
            }

            // call Operator (left, right)
            var method = UnsafeGetSpecialTypeMethod(syntax, member);
            Debug.Assert((object)method != null);

            return BoundCall.Synthesized(syntax, null, method, loweredLeft, loweredRight);
        }

        private BoundExpression MakeNullCheck(SyntaxNode syntax, BoundExpression rewrittenExpr, BinaryOperatorKind operatorKind)
        {
            Debug.Assert((operatorKind == BinaryOperatorKind.Equal) || (operatorKind == BinaryOperatorKind.NotEqual) ||
                (operatorKind == BinaryOperatorKind.NullableNullEqual) || (operatorKind == BinaryOperatorKind.NullableNullNotEqual));

            TypeSymbol exprType = rewrittenExpr.Type;

            // Don't even call this method if the expression cannot be nullable.
            Debug.Assert(
                (object)exprType == null ||
                exprType.IsNullableTypeOrTypeParameter() ||
                !exprType.IsValueType ||
                exprType.IsPointerType());

            TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);

            // Fold compile-time comparisons.
            if (rewrittenExpr.ConstantValue != null)
            {
                switch (operatorKind)
                {
                    case BinaryOperatorKind.Equal:
                        return MakeLiteral(syntax, ConstantValue.Create(rewrittenExpr.ConstantValue.IsNull, ConstantValueTypeDiscriminator.Boolean), boolType);
                    case BinaryOperatorKind.NotEqual:
                        return MakeLiteral(syntax, ConstantValue.Create(!rewrittenExpr.ConstantValue.IsNull, ConstantValueTypeDiscriminator.Boolean), boolType);
                }
            }

            TypeSymbol objectType = _compilation.GetSpecialType(SpecialType.System_Object);

            if ((object)exprType != null)
            {
                if (exprType.Kind == SymbolKind.TypeParameter)
                {
                    // Box type parameters.
                    rewrittenExpr = MakeConversionNode(syntax, rewrittenExpr, Conversion.Boxing, objectType, @checked: false);
                }
                else if (exprType.IsNullableType())
                {
                    operatorKind |= BinaryOperatorKind.NullableNull;
                }
            }

            return MakeBinaryOperator(
                syntax,
                operatorKind,
                rewrittenExpr,
                MakeLiteral(syntax, ConstantValue.Null, objectType),
                boolType,
                null);
        }

        /// <summary>
        /// Spec section 7.9: if the left operand is int or uint, mask the right operand with 0x1F;
        /// if the left operand is long or ulong, mask the right operand with 0x3F.
        /// </summary>
        private BoundExpression RewriteBuiltInShiftOperation(
            BoundBinaryOperator oldNode,
            SyntaxNode syntax,
            BinaryOperatorKind operatorKind,
            BoundExpression loweredLeft,
            BoundExpression loweredRight,
            TypeSymbol type,
            int rightMask)
        {
            SyntaxNode rightSyntax = loweredRight.Syntax;
            ConstantValue rightConstantValue = loweredRight.ConstantValue;
            TypeSymbol rightType = loweredRight.Type;
            Debug.Assert(rightType.SpecialType == SpecialType.System_Int32);

            if (rightConstantValue != null && rightConstantValue.IsIntegral)
            {
                int shiftAmount = rightConstantValue.Int32Value & rightMask;
                if (shiftAmount == 0)
                {
                    return loweredLeft;
                }

                loweredRight = MakeLiteral(rightSyntax, ConstantValue.Create(shiftAmount), rightType);
            }
            else
            {
                BinaryOperatorKind andOperatorKind = (operatorKind & ~BinaryOperatorKind.OpMask) | BinaryOperatorKind.And;
                loweredRight = new BoundBinaryOperator(
                    rightSyntax,
                    andOperatorKind,
                    null,
                    null,
                    LookupResultKind.Viable,
                    loweredRight,
                    MakeLiteral(rightSyntax, ConstantValue.Create(rightMask), rightType),
                    rightType);
            }

            return oldNode == null
                ? new BoundBinaryOperator(
                    syntax,
                    operatorKind,
                    null,
                    null,
                    LookupResultKind.Viable,
                    loweredLeft,
                    loweredRight,
                    type)
                : oldNode.Update(
                    operatorKind,
                    null,
                    null,
                    oldNode.ResultKind,
                    loweredLeft,
                    loweredRight,
                    type);
        }

        private BoundExpression RewritePointerNumericOperator(
            SyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression loweredLeft,
            BoundExpression loweredRight,
            TypeSymbol returnType,
            bool isPointerElementAccess,
            bool isLeftPointer)
        {
            if (isLeftPointer)
            {
                loweredRight = MakeSizeOfMultiplication(loweredRight, (PointerTypeSymbol)loweredLeft.Type, kind.IsChecked());
            }
            else
            {
                loweredLeft = MakeSizeOfMultiplication(loweredLeft, (PointerTypeSymbol)loweredRight.Type, kind.IsChecked());
            }

            if (isPointerElementAccess)
            {
                Debug.Assert(kind.Operator() == BinaryOperatorKind.Addition);

                // NOTE: This is here to persist a bug in Dev10.  checked(p[n]) should be equivalent to checked(*(p + n)),
                // but Dev10 omits the check on the addition (though it retains the check on the multiplication of n by
                // the size).
                kind = kind & ~BinaryOperatorKind.Checked;
            }

            return new BoundBinaryOperator(
                            syntax,
                            kind,
                            ConstantValue.NotAvailable,
                            null,
                            LookupResultKind.Viable,
                            loweredLeft,
                            loweredRight,
                            returnType);
        }

        /// <summary>
        /// This rather confusing method tries to reproduce the functionality of ExpressionBinder::bindPtrAddMul and
        /// ExpressionBinder::bindPtrMul.  The basic idea is that we have a numeric expression, x, and a pointer type, 
        /// T*, and we want to multiply x by sizeof(T).  Unfortunately, we need to stick in some conversions to make
        /// everything work.
        /// 
        ///   1) If x is an int, then convert it to an IntPtr (i.e. a native int).  Dev10 offers no explanation (ExpressionBinder::bindPtrMul).
        ///   2) Do overload resolution based on the (possibly converted) type of X and int (the type of sizeof(T)).
        ///   3) If the result type of the chosen multiplication operator is signed, convert the product to IntPtr;
        ///      otherwise, convert the product to UIntPtr.
        /// </summary>
        private BoundExpression MakeSizeOfMultiplication(BoundExpression numericOperand, PointerTypeSymbol pointerType, bool isChecked)
        {
            var sizeOfExpression = _factory.Sizeof(pointerType.PointedAtType);
            Debug.Assert(sizeOfExpression.Type.SpecialType == SpecialType.System_Int32);

            // Common case: adding or subtracting one  (e.g. for ++)
            if (numericOperand.ConstantValue?.UInt64Value == 1)
            {
                // We could convert this to a native int (as the unoptimized multiplication would be),
                // but that would be a no-op (int to native int), so don't bother.
                return sizeOfExpression;
            }

            var numericSpecialType = numericOperand.Type.SpecialType;

            // Optimization: the size is exactly one byte, then multiplication is unnecessary.
            if (sizeOfExpression.ConstantValue?.Int32Value == 1)
            {
                // As in ExpressionBinder::bindPtrAddMul, we apply the following conversions:
                //   int -> int (add allows int32 operands and will extend to native int if necessary)
                //   uint -> native uint (add will sign-extend 32bit operand on 64bit, we do not want that happening)
                //   long -> native int
                //   ulong -> native uint
                // Note that these are not the types we would see if we let the multiplication happen.
                // ACASEY: These rules are inferred from the native compiler.

                SpecialType destinationType = numericSpecialType;
                switch (numericSpecialType)
                {
                    case SpecialType.System_Int32:
                        // add operator can take int32 and extend to 64bit if necessary
                        // however in a case of checked operation, the operation is treated as unsigned with overflow ( add.ovf.un , sub.ovf.un )
                        // the IL spec is a bit vague whether JIT should sign or zero extend the shorter operand in such case
                        // and there could be inconsistencies in implementation or bugs.
                        // As a result, in checked contexts, we will force sign-extending cast to be sure
                        if (isChecked)
                        {
                            var constVal = numericOperand.ConstantValue;
                            if (constVal == null || constVal.Int32Value < 0)
                            {
                                destinationType = SpecialType.System_IntPtr;
                            }
                        }
                        break;
                    case SpecialType.System_UInt32:
                        {
                            // add operator treats operands as signed and will sign-extend on x64
                            // to prevent sign-extending, convert the operand to unsigned native int.
                            var constVal = numericOperand.ConstantValue;
                            if (constVal == null || constVal.UInt32Value > int.MaxValue)
                            {
                                destinationType = SpecialType.System_UIntPtr;
                            }
                        }
                        break;
                    case SpecialType.System_Int64:
                        destinationType = SpecialType.System_IntPtr;
                        break;
                    case SpecialType.System_UInt64:
                        destinationType = SpecialType.System_UIntPtr;
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(numericSpecialType);
                }

                return destinationType == numericSpecialType
                    ? numericOperand
                    : _factory.Convert(_factory.SpecialType(destinationType), numericOperand, Conversion.IntegerToPointer);
            }

            BinaryOperatorKind multiplicationKind = BinaryOperatorKind.Multiplication;

            TypeSymbol multiplicationResultType;
            TypeSymbol convertedMultiplicationResultType;
            switch (numericSpecialType)
            {
                case SpecialType.System_Int32:
                    {
                        TypeSymbol nativeIntType = _factory.SpecialType(SpecialType.System_IntPtr);

                        // From ExpressionBinder::bindPtrMul:
                        // this multiplication needs to be done as natural ints, but since a (int * natint) ==> natint,
                        // we only need to promote one side

                        numericOperand = _factory.Convert(nativeIntType, numericOperand, Conversion.IntegerToPointer, isChecked);
                        multiplicationKind |= BinaryOperatorKind.Int; //i.e. signed
                        multiplicationResultType = nativeIntType;
                        convertedMultiplicationResultType = nativeIntType;
                        break;
                    }
                case SpecialType.System_UInt32:
                    {
                        TypeSymbol longType = _factory.SpecialType(SpecialType.System_Int64);
                        TypeSymbol nativeIntType = _factory.SpecialType(SpecialType.System_IntPtr);

                        // We're multiplying a uint by an int, so promote both to long (same as normal operator overload resolution).
                        numericOperand = _factory.Convert(longType, numericOperand, Conversion.ExplicitNumeric, isChecked);
                        sizeOfExpression = _factory.Convert(longType, sizeOfExpression, Conversion.ExplicitNumeric, isChecked);
                        multiplicationKind |= BinaryOperatorKind.Long;
                        multiplicationResultType = longType;
                        convertedMultiplicationResultType = nativeIntType;
                        break;
                    }
                case SpecialType.System_Int64:
                    {
                        TypeSymbol longType = _factory.SpecialType(SpecialType.System_Int64);
                        TypeSymbol nativeIntType = _factory.SpecialType(SpecialType.System_IntPtr);

                        // We're multiplying a long by an int, so promote the int to long (same as normal operator overload resolution).
                        sizeOfExpression = _factory.Convert(longType, sizeOfExpression, Conversion.ExplicitNumeric, isChecked);
                        multiplicationKind |= BinaryOperatorKind.Long;
                        multiplicationResultType = longType;
                        convertedMultiplicationResultType = nativeIntType;
                        break;
                    }
                case SpecialType.System_UInt64:
                    {
                        TypeSymbol ulongType = _factory.SpecialType(SpecialType.System_UInt64);
                        TypeSymbol nativeUIntType = _factory.SpecialType(SpecialType.System_UIntPtr);

                        // We're multiplying a ulong by an int, so promote the int to ulong (same as normal operator overload resolution).
                        sizeOfExpression = _factory.Convert(ulongType, sizeOfExpression, Conversion.ExplicitNumeric, isChecked);
                        multiplicationKind |= BinaryOperatorKind.ULong;
                        multiplicationResultType = ulongType;
                        convertedMultiplicationResultType = nativeUIntType; //unsigned since multiplicationResultType is unsigned
                        break;
                    }
                default:
                    {
                        throw ExceptionUtilities.UnexpectedValue(numericSpecialType);
                    }
            }

            if (isChecked)
            {
                multiplicationKind |= BinaryOperatorKind.Checked;
            }
            var multiplication = _factory.Binary(multiplicationKind, multiplicationResultType, numericOperand, sizeOfExpression);
            return TypeSymbol.Equals(convertedMultiplicationResultType, multiplicationResultType, TypeCompareKind.ConsiderEverything2)
                ? multiplication
                : _factory.Convert(convertedMultiplicationResultType, multiplication, Conversion.IntegerToPointer); // NOTE: for some reason, dev10 doesn't check this conversion.
        }

        private BoundExpression RewritePointerSubtraction(
            BinaryOperatorKind kind,
            BoundExpression loweredLeft,
            BoundExpression loweredRight,
            TypeSymbol returnType)
        {
            Debug.Assert(loweredLeft.Type.IsPointerType());
            Debug.Assert(loweredRight.Type.IsPointerType());
            Debug.Assert(returnType.SpecialType == SpecialType.System_Int64);

            PointerTypeSymbol pointerType = (PointerTypeSymbol)loweredLeft.Type;
            var sizeOfExpression = _factory.Sizeof(pointerType.PointedAtType);

            // NOTE: to match dev10, the result of the subtraction is treated as an IntPtr
            // and then the result of the division is converted to long.
            // NOTE: dev10 doesn't optimize away division by 1.
            return _factory.Convert(
                returnType,
                _factory.Binary(
                    BinaryOperatorKind.Division,
                    _factory.SpecialType(SpecialType.System_IntPtr),
                    _factory.Binary(
                        kind & ~BinaryOperatorKind.Checked, // For some reason, dev10 never checks for subtraction overflow.
                        returnType,
                        loweredLeft,
                        loweredRight),
                    sizeOfExpression),
                Conversion.PointerToInteger);
        }
    }
}
