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
            var lhsRead = MakeRValue(transformedLHS);
            BoundExpression loweredRight = VisitExpression(node.RightOperand);

            // Now that LHS is transformed with temporaries, we rewrite this node into a coalesce expression:
            // lhsRead ?? (transformedLHS = loweredRight)

            // transformedLHS = rhs
            BoundExpression assignment = MakeAssignmentOperator(syntax, transformedLHS, loweredRight, node.LeftOperand.Type, used: true, node.IsChecked, isCompoundAssignment: true);

            // lhsRead ?? (transformedLHS = loweredRight)
            BoundExpression conditionalExpression = MakeNullCoalescingOperator(syntax, lhsRead, assignment, Conversion.Identity, node.LeftOperand.Type);

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
    }
}
