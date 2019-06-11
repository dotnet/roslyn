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
            var isValueTypeAssignment = node.IsNullableValueTypeAssignment;
            BoundExpression transformedLHS = TransformCompoundAssignmentLHS(node.LeftOperand, stores, temps, node.LeftOperand.HasDynamicType(), needsSpilling: isValueTypeAssignment);
            var lhsRead = MakeRValue(transformedLHS);
            BoundExpression loweredRight = VisitExpression(node.RightOperand);

            BoundExpression result =
                isValueTypeAssignment ?
                    rewriteNullCoalescingAssignmentForValueType() :
                    rewriteNullCoalscingAssignmentStandard();

            return result;

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
                // var tmp = lhsRead.GetValueOrDefault();
                // if (!lhsRead.HasValue) { tmp = loweredRight; transformedAssignment = tmp; }
                // tmp
                //
                // Except that a is only evaluated once.

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

                temps.Add(tmp.LocalSymbol);

                // tmp = loweredRight;
                var tmpAssignment =
                    MakeAssignmentOperator(node.Syntax, tmp, loweredRight, node.Type, used: true, isChecked: false, isCompoundAssignment: false);

                // transformedLhs = tmp;
                var transformedLhsAssignment =
                    MakeAssignmentOperator(
                        node.Syntax,
                        transformedLHS,
                        MakeConversionNode(tmp, transformedLHS.Type, @checked: false),
                        node.LeftOperand.Type,
                        used: true,
                        isChecked: false,
                        isCompoundAssignment: false); ;

                // !lhsRead.HasValue
                var lhsReadHasValue = BoundCall.Synthesized(leftOperand.Syntax, lhsRead, hasValue);
                var lhsReadHasValueNegation =
                    MakeUnaryOperator(UnaryOperatorKind.BoolLogicalNegation, leftOperand.Syntax, method: null, lhsReadHasValue, lhsReadHasValue.Type);

                // { tmp = b; transformedLhs = tmp; }
                var consequenceBuilder = ArrayBuilder<BoundStatement>.GetInstance(2);
                consequenceBuilder.Add(new BoundExpressionStatement(node.Syntax, tmpAssignment) { WasCompilerGenerated = true });
                consequenceBuilder.Add(new BoundExpressionStatement(node.Syntax, transformedLhsAssignment) { WasCompilerGenerated = true });
                var consequence = new BoundBlock(node.Syntax, locals: ImmutableArray<LocalSymbol>.Empty, consequenceBuilder.ToImmutableAndFree())
                {
                    WasCompilerGenerated = true
                };

                // if (!lhsRead.HasValue) { tmp = b; transformedLhs = tmp; }
                var ifHasValueStatement =
                    RewriteIfStatement(node.Syntax, lhsReadHasValueNegation, consequence, rewrittenAlternativeOpt: null, hasErrors: false);

                // Assemble final statement list, including any stores that were originally used
                // for capturing the lhs in a temp to start with.
                var statementsBuilder = ArrayBuilder<BoundStatement>.GetInstance(2 + stores.Count);
                foreach (var store in stores)
                {
                    statementsBuilder.Add(new BoundExpressionStatement(store.Syntax, store) { WasCompilerGenerated = true });
                }
                stores.Free();
                statementsBuilder.Add(new BoundExpressionStatement(leftOperand.Syntax, getValueOrDefaultStore) { WasCompilerGenerated = true });
                statementsBuilder.Add(ifHasValueStatement);

                _needsSpilling = true;
                return _factory.SpillSequence(temps.ToImmutableAndFree(), statementsBuilder.ToImmutableAndFree(), tmp);
            }
        }
    }
}
