// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var lhsRead = TransformNullCoalescingLHSRead(transformedLHS, stores, temps);
            BoundExpression loweredRight = VisitExpression(node.RightOperand);

            // Now that LHS is transformed with temporaries, we rewrite this node into a conditional expression:
            // (lhsRead != null) ? lhsRead : (transformedLHS = rhs)

            // lhsRead != null
            BoundExpression nullCheck = MakeNullCheck(syntax, lhsRead, BinaryOperatorKind.NotEqual);

            // transformedLHS = rhs
            BoundExpression assignment = MakeAssignmentOperator(syntax, transformedLHS, loweredRight, node.LeftOperand.Type, used: true, node.IsChecked, isCompoundAssignment: true);

            // (lhsRead != null) ? lhsRead : (transformedLHS = rhs)
            BoundExpression conditionalExpression = RewriteConditionalOperator(syntax, nullCheck, lhsRead, assignment, constantValueOpt: null, rewrittenType: node.LeftOperand.Type, isRef: false);

            BoundExpression result = (temps.Count == 0 && stores.Count == 0) ?
                    conditionalExpression :
                    new BoundSequence(
                        syntax,
                        temps.ToImmutable(),
                        stores.ToImmutable(),
                        conditionalExpression,
                        conditionalExpression.Type);

            temps.Free();
            stores.Free();

            return result;
        }

        private BoundExpression TransformNullCoalescingLHSRead(BoundExpression loweredLHS, ArrayBuilder<BoundExpression> stores, ArrayBuilder<LocalSymbol> temps)
        {
            // We want to make sure that we avoid re-evaluating property getters and the like if the value we find is non-null. If the
            // LHS can change between reads, then we store it to a temporary variable
            loweredLHS = MakeRValue(loweredLHS);
            if (CanChangeValueBetweenReads(loweredLHS))
            {
                var variableTemp = _factory.StoreToTemp(loweredLHS, out BoundAssignmentOperator store, refKind: loweredLHS.GetRefKind());
                stores.Add(store);
                temps.Add(variableTemp.LocalSymbol);
                loweredLHS = variableTemp;
            }
            return loweredLHS;
        }
    }
}
