// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitNullCoalescingAssignmentOperator(BoundNullCoalescingAssignmentOperator node)
        {
            SyntaxNode syntax = node.Syntax;
            var temps = ArrayBuilder<LocalSymbol>.GetInstance();
            var stores = ArrayBuilder<BoundExpression>.GetInstance();

            // Rewrite LHS with temporaries to prevent double-evaluation of side effects, as we'll need to use it multiple times.
            BoundExpression transformedLHS = TransformCompoundAssignmentLHS(node.LeftOperand, stores, temps, node.LeftOperand.HasDynamicType());
            var lhsRead = MakeRValue(transformedLHS);
            BoundExpression loweredRight = VisitExpression(node.RightOperand);

            return node.IsNullableValueTypeAssignment ?
                    rewriteNullCoalescingAssignmentForValueType() :
                    rewriteNullCoalscingAssignmentStandard();

            BoundExpression rewriteNullCoalscingAssignmentStandard()
            {
                // Now that LHS is transformed with temporaries, we rewrite this node into a coalesce expression:
                // lhsRead ?? (transformedLHS = loweredRight)

                // transformedLHS = loweredRight
                // isCompoundAssignment is only used for dynamic scenarios, and we want those scenarios to treat this like a standard assignment.
                // See CodeGenNullCoalescingAssignmentTests.CoalescingAssignment_DynamicRuntimeCastFailure, which will fail if
                // isCompoundAssignment is set to true. It will fail to throw a runtime binder cast exception.
                BoundExpression assignment = MakeAssignmentOperator(syntax, transformedLHS, loweredRight, node.LeftOperand.Type, used: true, isChecked: false, isCompoundAssignment: false);

                // lhsRead ?? (transformedLHS = loweredRight)
                BoundExpression conditionalExpression = MakeNullCoalescingOperator(syntax, lhsRead, assignment, Conversion.Identity, BoundNullCoalescingOperatorResultKind.LeftType, node.LeftOperand.Type);

                return (temps.Count == 0 && stores.Count == 0) ?
                    conditionalExpression :
                    new BoundSequence(
                        syntax,
                        temps.ToImmutableAndFree(),
                        stores.ToImmutableAndFree(),
                        conditionalExpression,
                        conditionalExpression.Type);
            }

            // Rewrites the null coalescing operator in the case where the result type is the underlying
            // non-nullable value type of the left side
            BoundExpression rewriteNullCoalescingAssignmentForValueType()
            {
                Debug.Assert(node.LeftOperand.Type.IsNullableType());
                Debug.Assert(node.Type.Equals(node.RightOperand.Type));

                // We lower the expression to this form:
                //
                // var tmp = lhsRead.GetValueOrDefault()
                // lhsRead.HasValue ? tmp : { /* sequence */ tmp = loweredRight; transformedLhs = tmp; tmp }

                var leftOperand = node.LeftOperand;
                if (!TryGetNullableMethod(leftOperand.Syntax,
                                          leftOperand.Type,
                                          SpecialMember.System_Nullable_T_GetValueOrDefault,
                                          out var getValueOrDefault))
                {
                    return BadExpression(node);
                }

                if (!TryGetNullableMethod(leftOperand.Syntax,
                                          leftOperand.Type,
                                          SpecialMember.System_Nullable_T_get_HasValue,
                                          out var hasValue))
                {
                    return BadExpression(node);
                }

                // If null coalescing assignment is supported in expression trees, the below code
                // will need to be updated to support property accesses as well as calls. Currently,
                // MakeRValue will never return a BoundPropertyAccess except in expression trees.
                Debug.Assert(!_inExpressionLambda && lhsRead.Kind != BoundKind.PropertyAccess);

                // If lhsRead is a call, such as to a property accessor, save the result off to a temp. This doesn't affect
                // the standard ??= case because it only uses lhsRead once.
                if (lhsRead.Kind == BoundKind.Call)
                {
                    var lhsTemp = _factory.StoreToTemp(lhsRead, out var store, kind: SynthesizedLocalKind.Spill);
                    stores.Add(store);
                    temps.Add(lhsTemp.LocalSymbol);
                    lhsRead = lhsTemp;
                }

                // tmp = lhsRead.GetValueOrDefault();
                var tmp = _factory.StoreToTemp(BoundCall.Synthesized(leftOperand.Syntax, lhsRead, getValueOrDefault),
                                               out var getValueOrDefaultStore,
                                               kind: SynthesizedLocalKind.Spill);

                stores.Add(getValueOrDefaultStore);
                temps.Add(tmp.LocalSymbol);

                // tmp = loweredRight;
                var tmpAssignment = MakeAssignmentOperator(node.Syntax, tmp, loweredRight, node.Type, used: true, isChecked: false, isCompoundAssignment: false);

                // transformedLhs = tmp;
                var transformedLhsAssignment =
                    MakeAssignmentOperator(
                        node.Syntax,
                        transformedLHS,
                        MakeConversionNode(tmp, transformedLHS.Type, @checked: false),
                        node.LeftOperand.Type,
                        used: true,
                        isChecked: false,
                        isCompoundAssignment: false);

                // lhsRead.HasValue
                var lhsReadHasValue = BoundCall.Synthesized(leftOperand.Syntax, lhsRead, hasValue);

                // { tmp = b; transformedLhs = tmp; tmp }
                var alternative = _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, ImmutableArray.Create(tmpAssignment, transformedLhsAssignment), tmp);

                // lhsRead.HasValue ? tmp : { /* sequence */ tmp = loweredRight; transformedLhs = tmp; tmp }
                var ternary = _factory.Conditional(lhsReadHasValue, tmp, alternative, tmp.Type);

                return _factory.Sequence(temps.ToImmutableAndFree(), stores.ToImmutableAndFree(), ternary);
            }
        }
    }
}
