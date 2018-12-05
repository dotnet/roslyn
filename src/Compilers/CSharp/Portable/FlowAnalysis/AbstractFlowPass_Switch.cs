// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class AbstractFlowPass<TLocalState>
    {
        #region implementation for the old-style (no-patterns) variation of the switch statement.

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            // visit switch header
            TLocalState breakState = VisitSwitchHeader(node);
            SetUnreachable();

            // visit switch block
            VisitSwitchBlock(node);
            Join(ref breakState, ref this.State);
            ResolveBreaks(breakState, node.BreakLabel);
            return null;
        }

        private TLocalState VisitSwitchHeader(BoundSwitchStatement node)
        {
            // Initial value for the Break state for a switch statement is established as follows:
            //  Break state = UnreachableState if either of the following is true:
            //  (1) there is a default label, or
            //  (2) the switch expression is constant and there is a matching case label.
            //  Otherwise, the Break state = current state.

            // visit switch expression
            VisitRvalue(node.Expression);
            TLocalState breakState = this.State;

            // For a switch statement, we simulate a possible jump to the switch labels to ensure that
            // the label is not treated as an unused label and a pending branch to the label is noted.

            // However, if switch expression is a constant, we must have determined the single target label
            // at bind time, i.e. node.ConstantTargetOpt, and we must simulate a jump only to this label.

            var constantTargetOpt = node.ConstantTargetOpt;
            if ((object)constantTargetOpt == null)
            {
                bool hasDefaultLabel = false;
                foreach (var section in node.SwitchSections)
                {
                    foreach (var boundSwitchLabel in section.SwitchLabels)
                    {
                        var label = boundSwitchLabel.Label;
                        hasDefaultLabel = hasDefaultLabel || boundSwitchLabel.ConstantValueOpt == null;
                        SetState(breakState.Clone());
                        var simulatedGoto = new BoundGotoStatement(node.Syntax, label);
                        VisitGotoStatement(simulatedGoto);
                    }
                }

                if (hasDefaultLabel)
                {
                    // Condition (1) for an unreachable break state is satisfied
                    breakState = UnreachableState();
                }
            }
            else if (!node.BreakLabel.Equals(constantTargetOpt))
            {
                SetState(breakState.Clone());
                var simulatedGoto = new BoundGotoStatement(node.Syntax, constantTargetOpt);
                VisitGotoStatement(simulatedGoto);

                // Condition (1) or (2) for an unreachable break state is satisfied
                breakState = UnreachableState();
            }

            return breakState;
        }

        private void VisitSwitchBlock(BoundSwitchStatement node)
        {
            var afterSwitchState = UnreachableState();
            var switchSections = node.SwitchSections;
            var iLastSection = (switchSections.Length - 1);
            // visit switch sections
            for (var iSection = 0; iSection <= iLastSection; iSection++)
            {
                VisitSwitchSection(switchSections[iSection], iSection == iLastSection);
                // Even though it is illegal for the end of a switch section to be reachable, in erroneous
                // code it may be reachable.  We treat that as an implicit break (branch to afterSwitchState).
                Join(ref afterSwitchState, ref this.State);
            }

            SetState(afterSwitchState);
        }

        public virtual BoundNode VisitSwitchSection(BoundSwitchSection node, bool lastSection)
        {
            return VisitSwitchSection(node);
        }

        public override BoundNode VisitSwitchSection(BoundSwitchSection node)
        {
            // visit switch section labels
            foreach (var boundSwitchLabel in node.SwitchLabels)
            {
                VisitRvalue(boundSwitchLabel.ExpressionOpt);
                VisitSwitchSectionLabel(boundSwitchLabel.Label, node);
            }

            // visit switch section body
            VisitStatementList(node);

            return null;
        }

        private void VisitSwitchSectionLabel(LabelSymbol label, BoundSwitchSection node)
        {
            VisitLabel(label, node);
        }

        #endregion implementation for the old-style (no-patterns) variation of the switch statement.

        #region implementation for the pattern-matching variation of the switch statement.

        public override BoundNode VisitPatternSwitchStatement(BoundPatternSwitchStatement node)
        {
            // visit switch header
            TLocalState breakState = VisitPatternSwitchHeader(node);

            // visit switch block
            VisitPatternSwitchBlock(node);
            Join(ref breakState, ref this.State);
            ResolveBreaks(breakState, node.BreakLabel);
            return null;
        }

        private void VisitPatternSwitchBlock(BoundPatternSwitchStatement node)
        {
            var initialState = this.State;
            var afterSwitchState = UnreachableState();
            var switchSections = node.SwitchSections;
            var iLastSection = (switchSections.Length - 1);

            // simulate the dispatch (setting pattern variables and jumping to labels) to
            // all reachable switch labels
            foreach (var section in switchSections)
            {
                foreach (var label in section.SwitchLabels)
                {
                    if (label.IsReachable && label != node.DefaultLabel)
                    {
                        SetState(initialState.Clone());
                        // assign pattern variables
                        VisitPattern(null, label.Pattern);
                        SetState(StateWhenTrue);
                        if (label.Guard != null)
                        {
                            VisitCondition(label.Guard);
                            SetState(StateWhenTrue);
                        }

                        PendingBranches.Add(new PendingBranch(label, this.State));
                    }
                }
            }

            // we always consider the default label reachable for flow analysis purposes
            // unless there was a single case that would match every input.
            if (node.DefaultLabel != null)
            {
                if (node.SomeLabelAlwaysMatches)
                {
                    SetUnreachable();
                }
                else
                {
                    SetState(initialState.Clone());
                }

                PendingBranches.Add(new PendingBranch(node.DefaultLabel, this.State));
            }

            // visit switch sections
            for (var iSection = 0; iSection <= iLastSection; iSection++)
            {
                VisitPatternSwitchSection(switchSections[iSection], node.Expression, iSection == iLastSection);
                // Even though it is illegal for the end of a switch section to be reachable, in erroneous
                // code it may be reachable.  We treat that as an implicit break (branch to afterSwitchState).
                Join(ref afterSwitchState, ref this.State);
            }

            SetState(afterSwitchState);
        }

        /// <summary>
        /// Visit the switch expression, and return the initial break state.
        /// </summary>
        private TLocalState VisitPatternSwitchHeader(BoundPatternSwitchStatement node)
        {
            // visit switch expression
            VisitRvalue(node.Expression);

            // return the exit state to use if no pattern matches
            if (FullyHandlesItsInput(node))
            {
                return UnreachableState();
            }
            else
            {
                return this.State;
            }
        }

        private bool FullyHandlesItsInput(BoundPatternSwitchStatement node)
        {
            // If the switch is complete (for example, because it has a default label),
            // just return true.
            if (node.IsComplete)
            {
                return true;
            }

            // We also check for completeness based on value. Other cases were handled in the construction of the decision tree.
            if (node.Expression.ConstantValue == null)
            {
                return false;
            }

            foreach (var section in node.SwitchSections)
            {
                foreach (var label in section.SwitchLabels)
                {
                    if (label.Guard != null && label.Guard.ConstantValue != ConstantValue.True)
                    {
                        continue;
                    }

                    if (label.Pattern.Kind == BoundKind.ConstantPattern &&
                        ((BoundConstantPattern)label.Pattern).ConstantValue == node.Expression.ConstantValue)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual void VisitPatternSwitchSection(BoundPatternSwitchSection node, BoundExpression switchExpression, bool isLastSection)
        {
            SetState(UnreachableState());
            foreach (var label in node.SwitchLabels)
            {
                VisitLabel(label.Label, node);
            }

            VisitStatementList(node);
        }

        #endregion implementation for the pattern-matching variation of the switch statement.
    }
}
