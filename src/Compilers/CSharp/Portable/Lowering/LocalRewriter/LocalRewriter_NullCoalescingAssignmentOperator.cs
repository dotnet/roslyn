// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Debug.Assert(node.Type is { });
            SyntaxNode syntax = node.Syntax;
            var temps = ArrayBuilder<LocalSymbol>.GetInstance();
            var stores = ArrayBuilder<BoundExpression>.GetInstance();
            Debug.Assert(node.LeftOperand.Type is { });

            // Rewrite LHS with temporaries to prevent double-evaluation of side effects, as we'll need to use it multiple times.
            BoundExpression transformedLHS = TransformCompoundAssignmentLHS(node.LeftOperand, stores, temps, node.LeftOperand.HasDynamicType());
            Debug.Assert(transformedLHS.Type is { });
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
                Debug.Assert(TypeSymbol.Equals(transformedLHS.Type, node.LeftOperand.Type, TypeCompareKind.AllIgnoreOptions));
                BoundExpression assignment;

                if (IsExtensionBlockMemberAccessWithByValPossiblyStructReceiver(transformedLHS))
                {
                    // We need to create a tree that ensures that receiver of 'set' is evaluated after the right hand side value
                    BoundLocal rightResult = _factory.StoreToTemp(loweredRight, out BoundAssignmentOperator assignmentToTemp, refKind: RefKind.None);
                    assignment = MakeAssignmentOperator(syntax, transformedLHS, rightResult, used: true, isChecked: false, AssignmentKind.NullCoalescingAssignment);
                    Debug.Assert(assignment.Type is { });
                    assignment = new BoundSequence(syntax, [rightResult.LocalSymbol], [assignmentToTemp], assignment, assignment.Type);
                }
                else
                {
                    assignment = MakeAssignmentOperator(syntax, transformedLHS, loweredRight, used: true, isChecked: false, AssignmentKind.NullCoalescingAssignment);
                }

                // lhsRead ?? (transformedLHS = loweredRight)
                var leftPlaceholder = new BoundValuePlaceholder(lhsRead.Syntax, lhsRead.Type);
                BoundExpression conditionalExpression = MakeNullCoalescingOperator(syntax, lhsRead, assignment, leftPlaceholder: leftPlaceholder, leftConversion: leftPlaceholder, BoundNullCoalescingOperatorResultKind.LeftType, node.LeftOperand.Type);
                Debug.Assert(conditionalExpression.Type is { });

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
                    var lhsTemp = _factory.StoreToTemp(lhsRead, out var store);
                    stores.Add(store);
                    temps.Add(lhsTemp.LocalSymbol);
                    lhsRead = lhsTemp;
                }

                // tmp = lhsRead.GetValueOrDefault();
                var tmp = _factory.StoreToTemp(BoundCall.Synthesized(leftOperand.Syntax, lhsRead, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, getValueOrDefault),
                                               out var getValueOrDefaultStore);

                stores.Add(getValueOrDefaultStore);
                temps.Add(tmp.LocalSymbol);

                // tmp = loweredRight;
                var tmpAssignment = MakeAssignmentOperator(node.Syntax, tmp, loweredRight, used: true, isChecked: false, AssignmentKind.SimpleAssignment);

                Debug.Assert(transformedLHS.Type.GetNullableUnderlyingType().Equals(tmp.Type.StrippedType(), TypeCompareKind.AllIgnoreOptions));

                // transformedLhs = tmp;
                Debug.Assert(TypeSymbol.Equals(transformedLHS.Type, node.LeftOperand.Type, TypeCompareKind.AllIgnoreOptions));
                var transformedLhsAssignment =
                    MakeAssignmentOperator(
                        node.Syntax,
                        transformedLHS,
                        MakeConversionNode(tmp, transformedLHS.Type, @checked: false, markAsChecked: true),
                        used: true,
                        isChecked: false,
                        AssignmentKind.NullCoalescingAssignment);

                // lhsRead.HasValue
                var lhsReadHasValue = BoundCall.Synthesized(leftOperand.Syntax, lhsRead, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, hasValue);

                // { tmp = b; transformedLhs = tmp; tmp }
                var alternative = _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, ImmutableArray.Create(tmpAssignment, transformedLhsAssignment), tmp);

                // lhsRead.HasValue ? tmp : { /* sequence */ tmp = loweredRight; transformedLhs = tmp; tmp }
                var ternary = _factory.Conditional(lhsReadHasValue, tmp, alternative, tmp.Type);

                return _factory.Sequence(temps.ToImmutableAndFree(), stores.ToImmutableAndFree(), ternary);
            }
        }
    }
}
