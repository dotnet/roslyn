// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class AbstractFlowPass<TLocalState>
    {
        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            // dispatch to the switch sections
            var initialState = VisitSwitchStatementDispatch(node);

            // visit switch sections
            var afterSwitchState = UnreachableState();
            var switchSections = node.SwitchSections;
            var iLastSection = (switchSections.Length - 1);
            for (var iSection = 0; iSection <= iLastSection; iSection++)
            {
                VisitSwitchSection(switchSections[iSection], iSection == iLastSection);
                // Even though it is illegal for the end of a switch section to be reachable, in erroneous
                // code it may be reachable.  We treat that as an implicit break (branch to afterSwitchState).
                Join(ref afterSwitchState, ref this.State);
            }

            if (node.DecisionDag.ReachableLabels.Contains(node.BreakLabel) ||
                (node.DefaultLabel == null && node.Expression.ConstantValue == null && IsTraditionalSwitch(node)))
            {
                Join(ref afterSwitchState, ref initialState);
            }

            ResolveBreaks(afterSwitchState, node.BreakLabel);

            return null;
        }

        protected virtual TLocalState VisitSwitchStatementDispatch(BoundSwitchStatement node)
        {
            // visit switch header
            VisitRvalue(node.Expression);

            TLocalState initialState = this.State.Clone();

            var reachableLabels = node.DecisionDag.ReachableLabels;
            foreach (var section in node.SwitchSections)
            {
                foreach (var label in section.SwitchLabels)
                {
                    if (reachableLabels.Contains(label.Label) || label.HasErrors ||
                        label == node.DefaultLabel && node.Expression.ConstantValue == null && IsTraditionalSwitch(node))
                    {
                        SetState(initialState.Clone());
                    }
                    else
                    {
                        SetUnreachable();
                    }

                    VisitPattern(label.Pattern);
                    SetState(StateWhenTrue);
                    if (label.WhenClause != null)
                    {
                        VisitCondition(label.WhenClause);
                        SetState(StateWhenTrue);
                    }

                    PendingBranches.Add(new PendingBranch(label, this.State, label.Label));
                }
            }

            return initialState;
        }

        /// <summary>
        /// Is the switch statement one that could be interpreted as a C# 6 or earlier switch statement?
        /// </summary>
        private bool IsTraditionalSwitch(BoundSwitchStatement node)
        {
            // Before recursive patterns were introduced, we did not consider handling both 'true' and 'false' to
            // completely handle all case of a switch on a bool unless there was some patterny syntax or semantics
            // in the switch.  We had two different bound nodes and separate flow analysis handling for
            // "traditional" switch statements and "pattern-based" switch statements.  We simulate that behavior
            // by testing to see if this switch would have been handled under the old rules by the old compiler.

            // If we are in a recent enough language version, we treat the switch as a fully pattern-based switch
            // for the purposes of flow analysis.
            if (compilation.LanguageVersion >= MessageID.IDS_FeatureRecursivePatterns.RequiredVersion())
            {
                return false;
            }

            if (!node.Expression.Type.IsValidV6SwitchGoverningType())
            {
                return false;
            }

            foreach (var sectionSyntax in ((SwitchStatementSyntax)node.Syntax).Sections)
            {
                foreach (var label in sectionSyntax.Labels)
                {
                    if (label.Kind() == SyntaxKind.CasePatternSwitchLabel)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        protected virtual void VisitSwitchSection(BoundSwitchSection node, bool isLastSection)
        {
            SetState(UnreachableState());
            foreach (var label in node.SwitchLabels)
            {
                VisitLabel(label.Label, node);
            }

            VisitStatementList(node);
        }

        public override BoundNode VisitSwitchDispatch(BoundSwitchDispatch node)
        {
            VisitRvalue(node.Expression);
            var state = this.State.Clone();
            PendingBranches.Add(new PendingBranch(node, state, node.DefaultLabel));
            foreach ((_, LabelSymbol label) in node.Cases)
            {
                PendingBranches.Add(new PendingBranch(node, state, label));
            }

            SetUnreachable();
            return null;
        }

        public override BoundNode VisitConvertedSwitchExpression(BoundConvertedSwitchExpression node)
        {
            return this.VisitSwitchExpression(node);
        }

        public override BoundNode VisitUnconvertedSwitchExpression(BoundUnconvertedSwitchExpression node)
        {
            return this.VisitSwitchExpression(node);
        }

        private BoundNode VisitSwitchExpression(BoundSwitchExpression node)
        {
            VisitRvalue(node.Expression);
            var dispatchState = this.State;
            var endState = UnreachableState();
            var reachableLabels = node.DecisionDag.ReachableLabels;
            foreach (var arm in node.SwitchArms)
            {
                SetState(dispatchState.Clone());
                VisitPattern(arm.Pattern);
                SetState(StateWhenTrue);
                if (!reachableLabels.Contains(arm.Label) || arm.Pattern.HasErrors)
                {
                    SetUnreachable();
                }

                if (arm.WhenClause != null)
                {
                    VisitCondition(arm.WhenClause);
                    SetState(StateWhenTrue);
                }

                VisitRvalue(arm.Value);
                Join(ref endState, ref this.State);
            }

            SetState(endState);
            return node;
        }
    }
}
