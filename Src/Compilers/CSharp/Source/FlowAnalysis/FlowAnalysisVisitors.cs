using Roslyn.Compilers.Internal;
using System.Collections.Generic;

namespace Roslyn.Compilers.CSharp
{
    // The visitors for the flow analysis pass
    partial class FlowAnalysisWalker : BoundTreeVisitor<object, object>
    {
        /// <summary>
        /// Since each language construct must be handled according to the rules of the language specification,
        /// the default visitor reports that the construct for the node is not implemented in the compiler.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        public override object DefaultVisit(BoundNode node, object arg)
        {
            return Unimplemented(node, "node kind '" + node.Kind + "' for flow analysis");
        }

        public override object VisitLocal(BoundLocal node, object arg)
        {
            // Note: the caller should avoid allowing this to be called for the left-hand-side of
            // an assignment (if a simple variable or this-qualified) or an out parameter.  That's
            // because this code assumes the variable is being read, not written.
            CheckAssigned(node.LocalSymbol, node.Syntax);
            return null;
        }

        public override object VisitLocalDeclaration(BoundLocalDeclaration node, object arg)
        {
            int slot = MakeSlot(node.LocalSymbol); // not initially assigned
            Assign(node, node.Initializer, !this.state.Reachable);
            if (node.Initializer != null)
            {
                VisitExpression(node.Initializer); // analyze the expression
                Assign(node, node.Initializer);
            }
            return null;
        }

        public override object VisitBlock(BoundBlock node, object arg)
        {
            var oldPending = SavePending();
            StartBlock(node);
            foreach (var statement in node.Statements)
                VisitStatement(statement);
            EndBlock(node);

            // scan for unresolved backward branches into labels of this block
            bool backwardBranchChanged = false;
            foreach (var statement in node.Statements)
            {
                var label = statement as BoundLabelStatement;
                if (label != null && ResolveBranches(label))
                    backwardBranchChanged = true;
            }
            this.backwardBranchChanged |= backwardBranchChanged;

            RestorePending(oldPending);
            return null;
        }

        // subclasses may override these if they need additional behavior on entry end exit from a block.
        protected virtual void StartBlock(BoundBlock block) { }
        protected virtual void EndBlock(BoundBlock block) { }

        public override object VisitExpressionStatement(BoundExpressionStatement node, object arg)
        {
            VisitExpression(node.Expression);
            return null;
        }

        public override object VisitCall(BoundCall node, object unused)
        {
            // If the method being called is a partial method without a definition, or is a conditional method
            // whose condition is not true, then the call has no effect and it is ignored for the purposes of
            // definite assignment analysis.  That means that subexpressions (including lambdas) are not even
            // analyzed.
            if (node.MethodSymbol.CallsAreOmitted(this.compilation))
                return null;

            VisitExpression(node.Receiver);
            foreach (var arg in node.Arguments)
            {
                if (arg.Kind == BoundKind.MakeRefOperator)
                {
                    Unimplemented(arg, "ref argument");
                    // TODO: if it is an "out" reference, bind as an lvalue
                }
                VisitExpression(arg);
            }
            return null;
        }

        public override object VisitTypeExpression(BoundTypeExpression node, object arg)
        {
            return null;
        }

        public override object VisitLiteral(BoundLiteral node, object arg)
        {
            return null;
        }

        public override object VisitConversion(BoundConversion node, object arg)
        {
            Visit(node.Operand);
            return null;
        }

        public override object VisitIfStatement(BoundIfStatement node, object arg)
        {
            // 5.3.3.5 If statements
            VisitCondition(node.Condition);
            var trueState = new FlowAnalysisLocalState(this.state.Reachable && !IsConstantFalse(node.Condition), state.AssignedWhenTrue);
            var falseState = new FlowAnalysisLocalState(this.state.Reachable && !IsConstantTrue(node.Condition), state.AssignedWhenFalse);
            this.state = trueState;
            VisitStatement(node.Consequence);
            trueState = this.state;
            this.state = falseState;
            if (node.Alternative != null)
                VisitStatement(node.Alternative);
            this.state.Join(trueState);
            return null;
        }

        public override object VisitReturnStatement(BoundReturnStatement node, object arg)
        {
            VisitExpression(node.Expression);
            this.pendingBranches.Add(new PendingBranch(node, this.state));
            SetUnreachable();
            return null;
        }

        public override object VisitThisReference(BoundThisReference node, object arg)
        {
            // TODO: in a struct constructor, "this" is not initially assigned.
            CheckAssigned(ThisSymbol, node.Syntax);
            return null;
        }

        public override object VisitPreviousSubmissionReference(BoundPreviousSubmissionReference node, object arg)
        {
            return null;
        }

        public override object VisitParameter(BoundParameter node, object arg)
        {
            CheckAssigned(node.ParameterSymbol, node.Syntax);
            return null;
        }

        public override object VisitObjectCreationExpression(BoundObjectCreationExpression node, object arg)
        {
            foreach (var e in node.Arguments)
                VisitExpression(e);
            return null;
        }

        public override object VisitAssignmentOperator(BoundAssignmentOperator node, object arg)
        {
            VisitLvalue(node.Left);
            VisitExpression(node.Right);
            Assign(node.Left, node.Right);
            return null;
        }

        public override object VisitFieldAccess(BoundFieldAccess node, object arg)
        {
            VisitExpression(node.Receiver);
            // TODO: special case for instance fields of structs
            return null;
        }

        public override object VisitPropertyAccess(BoundPropertyAccess node, object arg)
        {
            VisitExpression(node.Receiver);
            // TODO: special case for instance properties of structs
            return null;
        }

        public override object VisitMultipleLocalDeclarations(BoundMultipleLocalDeclarations node, object arg)
        {
            foreach (var v in node.LocalDeclarations)
                Visit(v);
            return null;
        }

        public override object VisitWhileStatement(BoundWhileStatement node, object arg)
        {
            // while (node.Condition) { node.Body; node.ContinueLabel: } node.BreakLabel:
            LoopHead(node);
            VisitCondition(node.Condition);
            var bodyState = new FlowAnalysisLocalState(this.state.Reachable && !IsConstantFalse(node.Condition), this.state.AssignedWhenTrue);
            var breakState = new FlowAnalysisLocalState(this.state.Reachable && !IsConstantTrue(node.Condition), this.state.AssignedWhenFalse);
            var oldPendingBranches = SavePending();
            this.state = bodyState;
            VisitStatement(node.Body);
            ResolveContinues(node.ContinueLabel);
            LoopTail(node);
            ResolveBreaks(oldPendingBranches, breakState, node.BreakLabel);
            return null;
        }

        public override object VisitArrayAccess(BoundArrayAccess node, object arg)
        {
            VisitExpression(node.Expression);
            foreach (var i in node.Indices)
                VisitExpression(i);
            return null;
        }

        public override object VisitBinaryOperator(BoundBinaryOperator node, object arg)
        {
            switch (node.OperatorKind)
            {
                case BinaryOperatorKind.LogicalBoolAnd:
                    {
                        VisitCondition(node.Left);
                        var leftState = this.state;
                        this.state = new FlowAnalysisLocalState(this.state.Reachable, leftState.AssignedWhenTrue);
                        VisitCondition(node.Right);
                        this.state.AssignedWhenFalse.IntersectWith(leftState.AssignedWhenFalse);
                        break;
                    }
                case BinaryOperatorKind.LogicalBoolOr:
                    {
                        VisitCondition(node.Left);
                        var leftState = this.state;
                        this.state = new FlowAnalysisLocalState(this.state.Reachable, leftState.AssignedWhenFalse);
                        VisitCondition(node.Right);
                        this.state.AssignedWhenTrue.IntersectWith(leftState.AssignedWhenTrue);
                        break;
                    }
                default:
                    VisitExpression(node.Left);
                    VisitExpression(node.Right);
                    break;
            }
            return null;
        }

        public override object VisitUnaryOperator(BoundUnaryOperator node, object arg)
        {
            if (node.OperatorKind == UnaryOperatorKind.BoolLogicalNegation)
            {
                // We have a special case for the ! unary operator, which can operate in a boolean context (5.3.3.26)
                VisitCondition(node.Operand);
                // it inverts the sense of assignedWhenTrue and assignedWhenFalse.
                this.state = new FlowAnalysisLocalState(this.state.Reachable, this.state.AssignedWhenFalse, this.state.AssignedWhenTrue);
            }
            else
            {
                VisitExpression(node.Operand);
            }
            return null;
        }

        public override object VisitArrayCreation(BoundArrayCreation node, object arg)
        {
            foreach (var e1 in node.Bounds)
                VisitExpression(e1);

            if (node.InitializerOpt != null && node.InitializerOpt.Initializers.IsNotNull)
            {
                foreach (var element in node.InitializerOpt.Initializers)
                    VisitExpression(element);
            }

            return null;
        }

        public override object VisitForStatement(BoundForStatement node, object arg)
        {
            VisitStatement(node.Initializer);
            LoopHead(node);
            bool isTrue;
            bool isFalse;
            if (node.Condition != null)
            {
                isFalse = IsConstantFalse(node.Condition);
                isTrue = IsConstantTrue(node.Condition);
                VisitCondition(node.Condition);
            }
            else
            {
                isTrue = true;
                isFalse = false;
                this.state = new FlowAnalysisLocalState(this.state.Reachable, this.state.Assigned, BitArray.AllSet(nextVariableSlot));
            }
            var bodyState = new FlowAnalysisLocalState(this.state.Reachable && !isFalse, this.state.AssignedWhenTrue);
            var breakState = new FlowAnalysisLocalState(this.state.Reachable && !isTrue, this.state.AssignedWhenFalse);
            var oldPendingBranches = this.pendingBranches;
            this.pendingBranches = ArrayBuilder<PendingBranch>.GetInstance();
            this.state = bodyState;
            VisitStatement(node.Body);
            ResolveContinues(node.ContinueLabel);
            VisitStatement(node.Increment);
            LoopTail(node);
            ResolveBreaks(oldPendingBranches, breakState, node.BreakLabel);
            return null;
        }

        public override object VisitForEachStatement(BoundForEachStatement node, object arg)
        {
            // foreach ( var v in node.Expression ) { node.Body; node.ContinueLabel: } node.BreakLabel:
            Unimplemented(node, "foreach statement"); // TODO: VisitExpression(node.Expression);
            var breakState = this.state.Clone();
            var oldPendingBranches = this.pendingBranches;
            this.pendingBranches = ArrayBuilder<PendingBranch>.GetInstance();
            LoopHead(node);
            VisitStatement(node.Body);
            ResolveContinues(node.ContinueLabel);
            LoopTail(node);
            ResolveBreaks(oldPendingBranches, breakState, node.BreakLabel);
            return null;
        }

        public override object VisitCompoundAdditionOperator(BoundCompoundAdditionOperator node, object arg)
        {
            return VisitCompoundOperator(node);
        }

        public override object VisitCompoundAndOperator(BoundCompoundAndOperator node, object arg)
        {
            return VisitCompoundOperator(node);
        }

        public override object VisitCompoundDivisionOperator(BoundCompoundDivisionOperator node, object arg)
        {
            return VisitCompoundOperator(node);
        }

        public override object VisitCompoundLeftShiftOperator(BoundCompoundLeftShiftOperator node, object arg)
        {
            return VisitCompoundOperator(node);
        }

        public override object VisitCompoundMultiplicationOperator(BoundCompoundMultiplicationOperator node, object arg)
        {
            return VisitCompoundOperator(node);
        }

        public override object VisitCompoundOrOperator(BoundCompoundOrOperator node, object arg)
        {
            return VisitCompoundOperator(node);
        }

        public override object VisitCompoundRemainderOperator(BoundCompoundRemainderOperator node, object arg)
        {
            return VisitCompoundOperator(node);
        }

        public override object VisitCompoundRightShiftOperator(BoundCompoundRightShiftOperator node, object arg)
        {
            return VisitCompoundOperator(node);
        }

        public override object VisitCompoundSubtractionOperator(BoundCompoundSubtractionOperator node, object arg)
        {
            return VisitCompoundOperator(node);
        }

        public override object VisitCompoundXOrOperator(BoundCompoundXOrOperator node, object arg)
        {
            return VisitCompoundOperator(node);
        }

        public override object VisitAsOperator(BoundAsOperator node, object arg)
        {
            VisitExpression(node.Operand);
            return null;
        }

        public override object VisitIsOperator(BoundIsOperator node, object arg)
        {
            VisitExpression(node.Operand);
            return null;
        }

        public override object VisitMethodGroup(BoundMethodGroup node, object arg)
        {
            // this should not occur in a properly bound tree.
            VisitExpression(node.Receiver);
            return null;
        }

        public override object VisitNullCoalescingOperator(BoundNullCoalescingOperator node, object arg)
        {
            VisitExpression(node.Left);
            if (IsConstantNull(node.Left))
            {
                VisitExpression(node.Right);
            }
            else
            {
                var savedState = this.state.Clone();
                VisitExpression(node.Right);
                this.state = savedState;
            }
            return null;
        }

        public override object VisitSequence(BoundSequence node, object arg)
        {
            var sideEffects = node.SideEffects;
            if (!sideEffects.IsNullOrEmpty)
            {
                foreach (var se in sideEffects)
                {
                    VisitExpression(se);
                }
            }

            VisitExpression(node.Value);
            return null;
        }

        public override object VisitSequencePoint(BoundSequencePoint node, object arg)
        {
            return null;
        }

        public override object VisitSequencePointWithSpan(BoundSequencePointWithSpan node, object arg)
        {
            return null;
        }

        public override object VisitStatementList(BoundStatementList node, object arg)
        {
            foreach (var statement in node.Statements)
            {
                Visit(statement);
            }

            return null;
        }

        public override object VisitUnboundLambda(UnboundLambda node, object arg)
        {
            // The presence of this node suggests an error detected in an earlier phase.
            return null;
        }

        public override object VisitBreakStatement(BoundBreakStatement node, object arg)
        {
            this.pendingBranches.Add(new PendingBranch(node, this.state));
            SetUnreachable();
            return null;
        }

        public override object VisitContinueStatement(BoundContinueStatement node, object arg)
        {
            // While continue statements do no affect definite assignment, subclasses
            // such as region flow analysis depend on their presence as pending branches.
            this.pendingBranches.Add(new PendingBranch(node, this.state));
            SetUnreachable();
            return null;
        }

        public override object VisitConditionalOperator(BoundConditionalOperator node, object arg)
        {
            VisitCondition(node.Condition);
            var consequenceState = new FlowAnalysisLocalState(this.state.Reachable, this.state.AssignedWhenTrue);
            var alternativeState = new FlowAnalysisLocalState(this.state.Reachable, this.state.AssignedWhenFalse);

            this.state = consequenceState;
            Visit(node.Consequence);
            consequenceState = this.state;
            
            this.state = alternativeState;
            Visit(node.Alternative);
            alternativeState = this.state;

            if (IsConstantTrue(node.Condition))
            {
                this.state = consequenceState;
                // it may be a boolean state at this point.
            }
            else if (IsConstantFalse(node.Condition))
            {
                this.state = alternativeState;
                // it may be a boolean state at this point.
            }
            else
            {
                // is may not be a boolean state at this point (5.3.3.28)
                this.state = consequenceState;
                this.state.Join(alternativeState);
            }
            return null;
        }

        public override object VisitBaseReference(BoundBaseReference node, object arg)
        {
            // TODO: in a struct constructor, "this" is not initially assigned.
            CheckAssigned(ThisSymbol, node.Syntax);
            return null;
        }

        public override object VisitDoStatement(BoundDoStatement node, object arg)
        {
            // do { statements; node.ContinueLabel: } while (node.Condition) node.BreakLabel:
            var oldPendingBranches = this.pendingBranches;
            this.pendingBranches = ArrayBuilder<PendingBranch>.GetInstance();
            LoopHead(node);
            VisitStatement(node.Body);
            ResolveContinues(node.ContinueLabel);
            VisitCondition(node.Condition);
            var breakState = new FlowAnalysisLocalState(this.state.Reachable && !IsConstantTrue(node.Condition), this.state.AssignedWhenFalse);
            this.state = new FlowAnalysisLocalState(this.state.Reachable && !IsConstantFalse(node.Condition), this.state.AssignedWhenTrue);
            LoopTail(node);
            ResolveBreaks(oldPendingBranches, breakState, node.BreakLabel);
            return null;
        }

        public override object VisitGotoStatement(BoundGotoStatement node, object arg)
        {
            pendingBranches.Add(new PendingBranch(node, this.state));
            SetUnreachable();
            return null;
        }

        public override object VisitLabelStatement(BoundLabelStatement node, object arg)
        {
            ResolveBranches(node);
            return null;
        }

        public override object VisitLockStatement(BoundLockStatement node, object arg)
        {
            VisitExpression(node.Argument);
            VisitStatement(node.Body);
            return null;
        }

        public override object VisitNoOpStatement(BoundNoOpStatement node, object arg)
        {
            return null;
        }

        public override object VisitNotYetImplemented(BoundNotYetImplemented node, object arg)
        {
            return null;
        }

        public override object VisitNamespaceExpression(BoundNamespaceExpression node, object arg)
        {
            return null;
        }

        public override object VisitUsingStatement(BoundUsingStatement node, object arg)
        {
            // TODO: should visit the expression, but that is missing from the bound using statement at the time this code was written.
            VisitStatement(node.Body);
            return null;
        }

#if false   // for better code coverage, commenting out until features are implemented
        public override object VisitAddressOperator(BoundAddressOperator node, object arg)
        {
            // this node type isn't even fleshed out.
            return Unimplemented(node, "addressof operator");
        }

        public override object VisitLambda(BoundLambda node, object arg)
        {
            var savedState = this.state.Clone();
            // TODO: the lambda node is missing fields as this code was written.
            Unimplemented(node, "lambda expression"); // TODO: Visit(node.Expression) OrderByClauseSyntax Visit(node.Statement);
            this.state = savedState;
            return null;
        }

        public override object VisitPointerDereferenceOperator(BoundPointerDereferenceOperator node, object arg)
        {
            // VisitExpression(node.Expression); // TODO: field missing from bound dereference operator
            Unimplemented(node, "pointer dereference");
            return null;
        }

        public override object VisitSwitchStatement(BoundSwitchStatement node, object arg)
        {
            VisitExpression(node.BoundExpression);
            // TODO: switch cases missing from bound switch node
            // TODO: make sure to take into account the possibility of constant switch expression
            // TODO: take into account the possibility of no switch blocks
            // TODO: give an error when the end of a switch block is reachable (falls through)
            Unimplemented(node, "switch statement");
            return null;
        }

        public override object VisitTryStatement(BoundTryStatement node, object arg)
        {
            var oldPending = SavePending();
            var initialState = this.state;
            this.state = initialState.Clone();
            VisitStatement(node.TryBlock);
            var finalState = this.state;
            foreach (var c in node.CatchBlocks)
            {
                this.state = initialState.Clone();
                VisitStatement(c.BoundBlock);
                finalState.Assigned.IntersectWith(this.state.Assigned);
                finalState.Reachable |= this.state.Reachable;
            }
            if (node.FinallyBlock != null)
            {
                this.state = initialState.Clone();
                VisitStatement(node.FinallyBlock); // this should generate no pending branches
                if (this.state.Reachable)
                {
                    foreach (var pend in this.pendingBranches)
                        pend.State.Assigned.UnionWith(this.state.Assigned);
                }
                else
                {
                    // the branches out are all intercepted
                    this.pendingBranches.Clear();
                }
                finalState.Assigned.UnionWith(this.state.Assigned);
                finalState.Reachable &= this.state.Reachable;
            }
            this.state = finalState;
            RestorePending(oldPending);
            return null;
        }

        public override object VisitSizeOfOperator(BoundSizeOfOperator node, object arg)
        {
            return null;
        }

        public override object VisitThrowStatement(BoundThrowStatement node, object arg)
        {
            VisitExpression(node.Expression);
            SetUnreachable();
            return null;
        }

        public override object VisitTypeOfOperator(BoundTypeOfOperator node, object arg)
        {
            return null;
        }
#endif
    }
}
