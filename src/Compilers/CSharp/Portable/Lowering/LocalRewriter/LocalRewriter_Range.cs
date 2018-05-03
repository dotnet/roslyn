// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        // PROTOTYPE: optimize for case where first operand is a conditional access

        public override BoundNode VisitRangeExpression(BoundRangeExpression node)
        {
            Debug.Assert(node != null);

            if (node.LeftOperand is null)
            {
                if (node.RightOperand is null)
                {
                    // Case 1:              ".."
                    // translates to:       Range.All()

                    MethodSymbol method = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Range__All);
                    return MakeCall(node.Syntax, rewrittenReceiver: null, method, ImmutableArray<BoundExpression>.Empty, method.ReturnType);
                }
                else
                {
                    MethodSymbol method = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Range__ToEnd);
                    BoundExpression rightOperand = VisitExpression(node.RightOperand);
                    rightOperand = NullableAlwaysHasValue(rightOperand) ?? rightOperand;

                    if (node.RightOperand.Type.IsNullableType())
                    {
                        // Case 2:              "..nullableRight"
                        // translates to:       nullableRight.HasValue ? new Nullable(Range.ToEnd(nullableRight.GetValueOrDefault())) : default

                        return LiftRangeExpression(node.Syntax, node.Type, method, rightOperand);
                    }
                    else
                    {
                        // Case 3:              "..right"
                        // translates to:       Range.ToEnd(right)

                        ImmutableArray<BoundExpression> arguments = ImmutableArray.Create(rightOperand);
                        return MakeCall(node.Syntax, rewrittenReceiver: null, method, arguments, method.ReturnType);
                    }
                }
            }
            else
            {
                BoundExpression leftOperand = VisitExpression(node.LeftOperand);
                leftOperand = NullableAlwaysHasValue(leftOperand) ?? leftOperand;

                if (node.RightOperand is null)
                {
                    MethodSymbol method = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Range__FromStart);

                    if (leftOperand.Type.IsNullableType())
                    {
                        // Case 4:              "nullableLeft.."
                        // translates to:       nullableLeft.HasValue ? new Nullable(Range.FromStart(nullableLeft.GetValueOrDefault())) : default

                        return LiftRangeExpression(node.Syntax, node.Type, method, leftOperand);
                    }
                    else
                    {
                        // Case 5:              "left.."
                        // translates to:       Range.FromStart(left)

                        ImmutableArray<BoundExpression> arguments = ImmutableArray.Create(leftOperand);
                        return MakeCall(node.Syntax, rewrittenReceiver: null, method, arguments, method.ReturnType);
                    }
                }
                else
                {
                    MethodSymbol method = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Range__Create);
                    BoundExpression rightOperand = VisitExpression(node.RightOperand);
                    rightOperand = NullableAlwaysHasValue(rightOperand) ?? rightOperand;

                    if (!leftOperand.Type.IsNullableType() && !rightOperand.Type.IsNullableType())
                    {
                        // Case 6:              "left..right"
                        // translates to:       Range.Create(left, right)

                        ImmutableArray<BoundExpression> arguments = ImmutableArray.Create(leftOperand, rightOperand);
                        return MakeCall(node.Syntax, rewrittenReceiver: null, method, arguments, method.ReturnType);
                    }
                    else
                    {
                        // Case 7:              "nullableLeft..nullableRight"
                        // translates to:       nullableLeft.HasValue && nullableRight.HasValue ? new Nullable(Range.Create(nullableLeft.GetValueOrDefault(), nullableRight.GetValueOrDefault())) : default

                        // Case 8:              "nullableLeft..right"
                        // translates to:       nullableLeft.HasValue ? new Nullable(Range.Create(nullableLeft.GetValueOrDefault(), right)) : default

                        // Case 9:              "left..nullableRight"
                        // translates to:       nullableRight.HasValue ? new Nullable(Range.Create(left, nullableRight.GetValueOrDefault())) : default

                        return LiftRangeExpression(node.Syntax, node.Type, method, leftOperand, rightOperand);
                    }
                }
            }
        }

        private BoundNode LiftRangeExpression(SyntaxNode syntax, TypeSymbol type, MethodSymbol method, params BoundExpression[] operands)
        {
            Debug.Assert(type.IsNullableType());
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
                        // PROTOTYPE: make sure this type exists in binding
                        TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);
                        condition = MakeBinaryOperator(syntax, BinaryOperatorKind.BoolAnd, condition, operandHasValue, boolType, method: null);
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
            BoundExpression rangeCall = MakeCall(syntax, rewrittenReceiver: null, method, arguments.ToImmutableAndFree(), method.ReturnType);

            // new Nullable(method(left.GetValueOrDefault(), right.GetValueOrDefault()))
            if (!TryGetNullableMethod(syntax, type, SpecialMember.System_Nullable_T__ctor, out MethodSymbol nullableCtor))
            {
                // PROTOTYPE: make sure this ctor exists in binding
                return BadExpression(syntax, type, arguments.ToImmutableArray());
            }

            BoundExpression consequence = new BoundObjectCreationExpression(syntax, nullableCtor, binderOpt: null, rangeCall);

            // default
            BoundExpression alternative = new BoundDefaultExpression(syntax, constantValueOpt: null, type);

            // left.HasValue && right.HasValue ? new Nullable(method(left.GetValueOrDefault(), right.GetValueOrDefault())) : default
            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: syntax,
                rewrittenCondition: condition,
                rewrittenConsequence: consequence,
                rewrittenAlternative: alternative,
                constantValueOpt: null,
                rewrittenType: type,
                isRef: false);

            // PROTOTYPE: comment from AlekseyTS: Because of this, lifted range operators should probably be disallowed in an expression tree context.
            return new BoundSequence(
                syntax: syntax,
                locals: locals.ToImmutableAndFree(),
                sideEffects: sideeffects.ToImmutableAndFree(),
                value: conditionalExpression,
                type: type);
        }
    }
}
