// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitRangeExpression(BoundRangeExpression node)
        {
            Debug.Assert(node != null && node.MethodOpt != null);

            bool needLifting = false;
            var operandsBuilder = new ArrayBuilder<BoundExpression>();

            var left = node.LeftOperand;
            if (left != null)
            {
                operandsBuilder.Add(tryOptimizeOperand(left));
            }

            var right = node.RightOperand;
            if (right != null)
            {
                operandsBuilder.Add(tryOptimizeOperand(right));
            }

            ImmutableArray<BoundExpression> operands = operandsBuilder.ToImmutable();

            if (needLifting)
            {
                return LiftRangeExpression(node, operands);
            }
            else
            {
                BoundExpression rangeCreation = MakeCall(
                    node.Syntax,
                    rewrittenReceiver: null,
                    node.MethodOpt,
                    operands,
                    node.MethodOpt.ReturnType.TypeSymbol);

                if (node.Type.IsNullableType())
                {
                    if (!TryGetNullableMethod(node.Syntax, node.Type, SpecialMember.System_Nullable_T__ctor, out MethodSymbol nullableCtor))
                    {
                        return BadExpression(node.Syntax, node.Type, node);
                    }

                    return new BoundObjectCreationExpression(node.Syntax, nullableCtor, binderOpt: null, rangeCreation);
                }

                return rangeCreation;
            }

            BoundExpression tryOptimizeOperand(BoundExpression operand)
            {
                Debug.Assert(operand != null);
                operand = VisitExpression(operand);

                if (NullableNeverHasValue(operand))
                {
                    operand = new BoundDefaultExpression(operand.Syntax, operand.Type.GetNullableUnderlyingType());
                }
                else
                {
                    operand = NullableAlwaysHasValue(operand) ?? operand;

                    if (operand.Type.IsNullableType())
                    {
                        needLifting = true;
                    }
                }

                return operand;
            }
        }

        private BoundExpression LiftRangeExpression(BoundRangeExpression node, ImmutableArray<BoundExpression> operands)
        {
            Debug.Assert(node.Type.IsNullableType());
            Debug.Assert(operands.Any(operand => operand.Type.IsNullableType()));
            Debug.Assert(operands.Length == 1 || operands.Length == 2);

            ArrayBuilder<BoundExpression> sideeffects = ArrayBuilder<BoundExpression>.GetInstance();
            ArrayBuilder<LocalSymbol> locals = ArrayBuilder<LocalSymbol>.GetInstance();
            ArrayBuilder<BoundExpression> arguments = ArrayBuilder<BoundExpression>.GetInstance();

            // left.HasValue && right.HasValue
            BoundExpression condition = null;
            foreach (var operand in operands)
            {
                BoundExpression tempOperand = CaptureExpressionInTempIfNeeded(operand, sideeffects, locals);

                if (tempOperand.Type.IsNullableType())
                {
                    BoundExpression operandHasValue = MakeOptimizedHasValue(tempOperand.Syntax, tempOperand);

                    if (condition is null)
                    {
                        condition = operandHasValue;
                    }
                    else
                    {
                        TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);
                        condition = MakeBinaryOperator(node.Syntax, BinaryOperatorKind.BoolAnd, condition, operandHasValue, boolType, method: null);
                    }

                    arguments.Add(MakeOptimizedGetValueOrDefault(tempOperand.Syntax, tempOperand));
                }
                else
                {
                    arguments.Add(tempOperand);
                }
            }

            Debug.Assert(condition != null);

            // method(left.GetValueOrDefault(), right.GetValueOrDefault())
            BoundExpression rangeCall = MakeCall(
                node.Syntax,
                rewrittenReceiver: null,
                node.MethodOpt,
                arguments.ToImmutableArray(),
                node.MethodOpt.ReturnType.TypeSymbol);

            if (!TryGetNullableMethod(node.Syntax, node.Type, SpecialMember.System_Nullable_T__ctor, out MethodSymbol nullableCtor))
            {
                return BadExpression(node.Syntax, node.Type, node);
            }

            // new Nullable(method(left.GetValueOrDefault(), right.GetValueOrDefault()))
            BoundExpression consequence = new BoundObjectCreationExpression(node.Syntax, nullableCtor, binderOpt: null, rangeCall);

            // default
            BoundExpression alternative = new BoundDefaultExpression(node.Syntax, constantValueOpt: null, node.Type);

            // left.HasValue && right.HasValue ? new Nullable(method(left.GetValueOrDefault(), right.GetValueOrDefault())) : default
            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: node.Syntax,
                rewrittenCondition: condition,
                rewrittenConsequence: consequence,
                rewrittenAlternative: alternative,
                constantValueOpt: null,
                rewrittenType: node.Type,
                isRef: false);

            return new BoundSequence(
                syntax: node.Syntax,
                locals: locals.ToImmutableAndFree(),
                sideEffects: sideeffects.ToImmutableAndFree(),
                value: conditionalExpression,
                type: node.Type);
        }
    }
}
