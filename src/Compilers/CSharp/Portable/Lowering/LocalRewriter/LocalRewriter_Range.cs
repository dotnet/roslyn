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
        public override BoundNode VisitRangeExpression(BoundRangeExpression node)
        {
            if (node is null)
            {
                return null;
            }

            if (node.LeftOperand is null)
            {
                if (node.RightOperand is null)
                {
                    // Case 1:              ".."
                    // translates to:       Range.All()

                    MethodSymbol method = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Range__All);
                    return MakeCall(node.Syntax, rewrittenReceiver: null, method, ImmutableArray<BoundExpression>.Empty, node.Type);
                }

                BoundExpression rightOperand = VisitExpression(node.RightOperand);
                rightOperand = NullableAlwaysHasValue(rightOperand) ?? rightOperand;

                if (node.RightOperand.Type.IsNullableType())
                {
                    // Case 2:              "..nullableRight"
                    // translates to:       nullableRight.HasValue ? new Nullable(Range.ToEnd(nullableRight.GetValueOrDefault())) : default

                    MethodSymbol method = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Range__ToEnd);
                    return LiftRangeExpression(node.Syntax, node.Type, method, rightOperand);
                }
                else
                {
                    // Case 3:              "..right"
                    // translates to:       Range.ToEnd(right)

                    MethodSymbol method = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Range__ToEnd);
                    ImmutableArray<BoundExpression> arguments = ImmutableArray.Create(rightOperand);
                    return MakeCall(node.Syntax, rewrittenReceiver: null, method, arguments, node.Type);
                }
            }
            else
            {
                BoundExpression leftOperand = VisitExpression(node.LeftOperand);
                leftOperand = NullableAlwaysHasValue(leftOperand) ?? leftOperand;

                if (node.RightOperand is null)
                {
                    if (leftOperand.Type.IsNullableType())
                    {
                        // Case 4:              "nullableLeft.."
                        // translates to:       nullableLeft.HasValue ? new Nullable(Range.FromStart(nullableLeft.GetValueOrDefault())) : default

                        MethodSymbol method = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Range__FromStart);
                        return LiftRangeExpression(node.Syntax, node.Type, method, leftOperand);
                    }
                    else
                    {
                        // Case 5:              "left.."
                        // translates to:       Range.FromStart(left)

                        MethodSymbol method = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Range__FromStart);
                        ImmutableArray<BoundExpression> arguments = ImmutableArray.Create(leftOperand);
                        return MakeCall(node.Syntax, rewrittenReceiver: null, method, arguments, node.Type);
                    }
                }

                BoundExpression rightOperand = VisitExpression(node.RightOperand);
                rightOperand = NullableAlwaysHasValue(rightOperand) ?? rightOperand;

                if (!leftOperand.Type.IsNullableType() && !rightOperand.Type.IsNullableType())
                {
                    // Case 6:              "left..right"
                    // translates to:       Range.Create(left, right)

                    MethodSymbol method = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Range__Create);
                    ImmutableArray<BoundExpression> arguments = ImmutableArray.Create(leftOperand, rightOperand);
                    return MakeCall(node.Syntax, rewrittenReceiver: null, method, arguments, node.Type);
                }
                else
                {
                    // Case 7:              "nullableLeft..nullableRight"
                    // translates to:       nullableLeft.HasValue && nullableRight.HasValue ? new Nullable(Range.Create(nullableLeft.GetValueOrDefault(), nullableRight.GetValueOrDefault())) : default

                    // Case 8:              "nullableLeft..right"
                    // translates to:       nullableLeft.HasValue ? new Nullable(Range.Create(nullableLeft.GetValueOrDefault(), right)) : default

                    // Case 9:              "left..nullableRight"
                    // translates to:       nullableRight.HasValue ? new Nullable(Range.Create(left, nullableRight.GetValueOrDefault())) : default

                    MethodSymbol method = (MethodSymbol)_compilation.GetWellKnownTypeMember(WellKnownMember.System_Range__Create);
                    return LiftRangeExpression(node.Syntax, node.Type, method, leftOperand, rightOperand);
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
            for (var i = 0; i < operands.Length; i++)
            {
                BoundExpression operand = operands[i];
                if (operand.Type.IsNullableType())
                {
                    operand = CaptureExpressionInTempIfNeeded(operand, sideeffects, locals);

                    if (condition is null)
                    {
                        condition = MakeOptimizedHasValue(operand.Syntax, operand);
                    }
                    else
                    {
                        TypeSymbol boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);
                        BoundExpression operandHasValue = MakeOptimizedHasValue(operand.Syntax, operand);
                        condition = MakeBinaryOperator(syntax, BinaryOperatorKind.BoolAnd, condition, operandHasValue, boolType, method: null);
                    }

                    arguments.Add(MakeOptimizedGetValueOrDefault(operand.Syntax, operand));
                }
                else
                {
                    arguments.Add(operand);
                }
            }

            Debug.Assert(condition != null);

            // method(left.GetValueOrDefault(), right.GetValueOrDefault())
            BoundExpression rangeCall = MakeCall(syntax, rewrittenReceiver: null, method, arguments.ToImmutableAndFree(), type);

            // new Nullable(method(left.GetValueOrDefault(), right.GetValueOrDefault()))
            MethodSymbol nullableCtor = (MethodSymbol)_compilation.GetSpecialTypeMember(SpecialMember.System_Nullable_T__ctor).SymbolAsMember((NamedTypeSymbol)type);
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

            return new BoundSequence(
                syntax: syntax,
                locals: locals.ToImmutableAndFree(),
                sideEffects: sideeffects.ToImmutableAndFree(),
                value: conditionalExpression,
                type: type);
        }
    }
}
