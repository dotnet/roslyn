// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class PreciseAbstractFlowPass<LocalState>
    {
        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            // visit switch header
            LocalState breakState = VisitSwitchHeader(node);
            SetUnreachable();

            // visit switch block
            VisitSwitchBlock(node);
            IntersectWith(ref breakState, ref this.State);
            ResolveBreaks(breakState, node.BreakLabel);
            return null;
        }

        private LocalState VisitSwitchHeader(BoundSwitchStatement node)
        {
            // Initial value for the Break state for a switch statement is established as follows:
            //  Break state = UnreachableState if either of the following is true:
            //  (1) there is a default label, or
            //  (2) the switch expression is constant and there is a matching case label.
            //  Otherwise, the Break state = current state.

            // visit switch expression
            VisitRvalue(node.Expression);
            LocalState breakState = this.State;

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
                        hasDefaultLabel = hasDefaultLabel || label.IdentifierNodeOrToken.Kind() == SyntaxKind.DefaultSwitchLabel;
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
                IntersectWith(ref afterSwitchState, ref this.State);
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

        protected virtual void VisitSwitchSectionLabel(LabelSymbol label, BoundSwitchSection node)
        {
            VisitLabel(label, node);
        }

        // ===========================
        // Below here is the implementation for the pattern-matching variation of the switch statement.
        // ===========================

        public override BoundNode VisitMatchStatement(BoundMatchStatement node)
        {
            // visit switch header
            LocalState breakState = VisitMatchHeader(node);

            // visit switch block
            VisitMatchBlock(node);
            IntersectWith(ref breakState, ref this.State);
            ResolveBreaks(breakState, node.BreakLabel);
            return null;
        }

        private void VisitMatchBlock(BoundMatchStatement node)
        {
            var afterSwitchState = UnreachableState();
            var switchSections = node.MatchSections;
            var iLastSection = (switchSections.Length - 1);
            var dispatchState = State.Clone();

            // visit switch sections
            for (var iSection = 0; iSection <= iLastSection; iSection++)
            {
                SetState(dispatchState);
                VisitMatchSection(switchSections[iSection], iSection == iLastSection);
                // Even though it is illegal for the end of a switch section to be reachable, in erroneous
                // code it may be reachable.  We treat that as an implicit break (branch to afterSwitchState).
                IntersectWith(ref afterSwitchState, ref this.State);
            }

            SetState(afterSwitchState);
        }

        private LocalState VisitMatchHeader(BoundMatchStatement node)
        {
            // Initial value for the Break state for a switch statement is established as follows:
            //  Break state = UnreachableState if either of the following is true:
            //  (1) there is a default label, or
            //  (2) the switch expression is constant and there is a matching case label.
            //  Otherwise, the Break state = current state.

            // visit switch expression
            VisitRvalue(node.Expression);
            LocalState breakState = this.State;

            // TODO: handle the switch expression being a constant.

            return breakState;
        }

        private void VisitMatchSection(BoundMatchSection node, bool v)
        {
            // visit switch section labels
            foreach (var label in node.MatchLabels)
            {
                // VisitPattern(label.Pattern); // TODO: implement this
                VisitRvalue(label.Guard);
            }

            // visit switch section body
            VisitStatementList(node);
        }
    }
}
