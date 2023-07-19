// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class MayHaveSideEffectsVisitor : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        private bool _mayHaveSideEffects;

        internal static bool MayHaveSideEffects(BoundNode node)
        {
            var visitor = new MayHaveSideEffectsVisitor();
            visitor.Visit(node);
            return visitor._mayHaveSideEffects;
        }

        public override BoundNode Visit(BoundNode node)
        {
            if (_mayHaveSideEffects)
            {
                return null;
            }
            return base.Visit(node);
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            return this.SetMayHaveSideEffects();
        }

        public override BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            return this.SetMayHaveSideEffects();
        }

        // Calls are treated as having side effects, but properties and
        // indexers are not. (Since this visitor is run on the bound tree
        // before lowering, properties are not represented as calls.)
        protected override void VisitArguments(BoundCall node)
        {
            this.SetMayHaveSideEffects();
        }

        public override BoundNode VisitDynamicInvocation(BoundDynamicInvocation node)
        {
            return this.SetMayHaveSideEffects();
        }

        public override BoundNode VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            return this.SetMayHaveSideEffects();
        }

        public override BoundNode VisitEventAssignmentOperator(BoundEventAssignmentOperator node)
        {
            return this.SetMayHaveSideEffects();
        }

        public override BoundNode VisitIncrementOperator(BoundIncrementOperator node)
        {
            return this.SetMayHaveSideEffects();
        }

        public override BoundNode VisitObjectInitializerExpression(BoundObjectInitializerExpression node)
        {
            foreach (var initializer in node.Initializers)
            {
                // Do not treat initializer assignment as a side effect since it is
                // part of an object creation. In short, visit the RHS only.
                var expr = (initializer.Kind == BoundKind.AssignmentOperator)
                    ? ((BoundAssignmentOperator)initializer).Right
                    : initializer;
                this.Visit(expr);
            }
            return null;
        }

        private BoundNode SetMayHaveSideEffects()
        {
            _mayHaveSideEffects = true;
            return null;
        }
    }
}
