// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Linq;
using Roslyn.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitRangeExpression(BoundRangeExpression node)
        {
            Debug.Assert(node != null && node.MethodOpt != null);

            bool needLifting = false;
            var F = _factory;

            var left = node.LeftOperandOpt;
            if (left != null)
            {
                left = tryOptimizeOperand(left);
            }

            var right = node.RightOperandOpt;
            if (right != null)
            {
                right = tryOptimizeOperand(right);
            }

            if (needLifting)
            {
                return LiftRangeExpression(node, left, right);
            }
            else
            {
                BoundExpression rangeCreation = MakeRangeExpression(node.MethodOpt, left, right);

                if (node.Type.IsNullableType())
                {
                    return ConvertToNullable(node.Syntax, node.Type, rangeCreation);
                }

                return rangeCreation;
            }

            BoundExpression tryOptimizeOperand(BoundExpression operand)
            {
                Debug.Assert(operand != null);
                operand = VisitExpression(operand);
                Debug.Assert(operand.Type is { });

                if (NullableNeverHasValue(operand))
                {
                    operand = new BoundDefaultExpression(operand.Syntax, operand.Type.GetNullableUnderlyingType());
                }
                else
                {
                    operand = NullableAlwaysHasValue(operand) ?? operand;
                    Debug.Assert(operand.Type is { });

                    if (operand.Type.IsNullableType())
                    {
                        needLifting = true;
                    }
                }

                return operand;
            }
        }

        private BoundExpression LiftRangeExpression(BoundRangeExpression node, BoundExpression? left, BoundExpression? right)
        {
            Debug.Assert(node.Type.IsNullableType());
            Debug.Assert(left?.Type?.IsNullableType() == true || right?.Type?.IsNullableType() == true);
            Debug.Assert(!(left is null && right is null));
            Debug.Assert(node.MethodOpt is { });

            var sideeffects = ArrayBuilder<BoundExpression>.GetInstance();
            var locals = ArrayBuilder<LocalSymbol>.GetInstance();

            // makeRange(left.GetValueOrDefault(), right.GetValueOrDefault())
            BoundExpression? condition = null;
            left = getIndexFromPossibleNullable(left);
            right = getIndexFromPossibleNullable(right);
            var rangeExpr = MakeRangeExpression(node.MethodOpt, left, right);

            Debug.Assert(condition != null);

            if (!TryGetNullableMethod(node.Syntax, node.Type, SpecialMember.System_Nullable_T__ctor, out MethodSymbol nullableCtor))
            {
                return BadExpression(node.Syntax, node.Type, node);
            }

            // new Nullable(makeRange(left.GetValueOrDefault(), right.GetValueOrDefault()))
            BoundExpression consequence = new BoundObjectCreationExpression(node.Syntax, nullableCtor, rangeExpr);

            // default
            BoundExpression alternative = new BoundDefaultExpression(node.Syntax, node.Type);

            // left.HasValue && right.HasValue
            //     ? new Nullable(makeRange(left.GetValueOrDefault(), right.GetValueOrDefault()))
            //     : default
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

            BoundExpression? getIndexFromPossibleNullable(BoundExpression? arg)
            {
                if (arg is null)
                    return null;

                BoundExpression tempOperand = CaptureExpressionInTempIfNeeded(arg, sideeffects, locals);
                Debug.Assert(tempOperand.Type is { });

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
                        condition = MakeBinaryOperator(node.Syntax, BinaryOperatorKind.BoolAnd, condition, operandHasValue, boolType, method: null, constrainedToTypeOpt: null);
                    }

                    return MakeOptimizedGetValueOrDefault(tempOperand.Syntax, tempOperand);
                }
                else
                {
                    return tempOperand;
                }
            }
        }

        private BoundExpression MakeRangeExpression(
            MethodSymbol constructionMethod,
            BoundExpression? left,
            BoundExpression? right)
        {
            var F = _factory;
            // The construction method may vary based on what well-known
            // members were available during binding. Depending on which member
            // is chosen we need to change our adjust our calling node.
            switch (constructionMethod.MethodKind)
            {
                case MethodKind.Constructor:
                    // Represents Range..ctor(Index left, Index right)
                    // The constructor can always be used to construct a range,
                    // but if any of the arguments are missing then we need to
                    // construct replacement Indexes
                    left = left ?? newIndexZero(fromEnd: false);
                    right = right ?? newIndexZero(fromEnd: true);

                    return F.New(constructionMethod, ImmutableArray.Create(left, right));

                case MethodKind.Ordinary:
                    // Represents either Range.StartAt or Range.EndAt, which
                    // means that the `..` expression is missing an argument on
                    // either the left or the right (i.e., `x..` or `..x`)
                    Debug.Assert(left is null ^ right is null);
                    Debug.Assert(constructionMethod.MetadataName == "StartAt" ||
                                 constructionMethod.MetadataName == "EndAt");
                    Debug.Assert(constructionMethod.IsStatic);
                    var arg = left ?? right;
                    Debug.Assert(arg is { });
                    return F.StaticCall(constructionMethod, ImmutableArray.Create(arg));

                case MethodKind.PropertyGet:
                    // The only property is Range.All, so the expression must
                    // be `..` with both arguments missing
                    Debug.Assert(constructionMethod.MetadataName == "get_All");
                    Debug.Assert(constructionMethod.IsStatic);
                    Debug.Assert(left is null && right is null);
                    return F.StaticCall(constructionMethod, ImmutableArray<BoundExpression>.Empty);

                default:
                    throw ExceptionUtilities.UnexpectedValue(constructionMethod.MethodKind);
            }

            BoundExpression newIndexZero(bool fromEnd) =>
                // new Index(0, fromEnd: fromEnd)
                F.New(
                    WellKnownMember.System_Index__ctor,
                    ImmutableArray.Create<BoundExpression>(F.Literal(0), F.Literal(fromEnd)));
        }
    }
}
