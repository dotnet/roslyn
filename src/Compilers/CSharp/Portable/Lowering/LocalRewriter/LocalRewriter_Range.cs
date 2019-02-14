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
            var F = _factory;

            var left = node.LeftOperand;
            if (left != null)
            {
                left = tryOptimizeOperand(left);
            }
            else
            {
                left = newIndexZero(fromEnd: false);
            }

            var right = node.RightOperand;
            if (right != null)
            {
                right = tryOptimizeOperand(right);
            }
            else
            {
                right = newIndexZero(fromEnd: true);
            }

            var operands = ImmutableArray.Create(left, right);

            if (needLifting)
            {
                return LiftRangeExpression(node, operands);
            }
            else
            {
                BoundExpression rangeCreation = new BoundObjectCreationExpression(
                    node.Syntax,
                    node.MethodOpt,
                    binderOpt: null,
                    operands);

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

            BoundExpression newIndexZero(bool fromEnd) =>
                // new Index(0, fromEnd: fromEnd)
                F.New(
                    WellKnownMember.System_Index__ctor,
                    ImmutableArray.Create<BoundExpression>(F.Literal(0), F.Literal(fromEnd)));

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
            BoundExpression rangeCall = new BoundObjectCreationExpression(
                node.Syntax,
                node.MethodOpt,
                binderOpt: null,
                arguments.ToImmutableArray());

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
