﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class PreciseAbstractFlowPass<LocalState>
    {
        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            // visit switch header
            VisitRvalue(node.Expression);

            // visit switch block
            VisitSwitchBlock(node);

            return null;
        }

        private void VisitSwitchBlock(BoundSwitchStatement node)
        {
            var initialState = State.Clone();
            var reachableLabels = node.DecisionDag.ReachableLabels;
            foreach (var section in node.SwitchSections)
            {
                foreach (var label in section.SwitchLabels)
                {
                    if (reachableLabels.Contains(label.Label) || label.HasErrors)
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

                    _pendingBranches.Add(new PendingBranch(label, this.State, label.Label));
                }
            }

            // visit switch sections
            var afterSwitchState = UnreachableState();
            var switchSections = node.SwitchSections;
            var iLastSection = (switchSections.Length - 1);
            for (var iSection = 0; iSection <= iLastSection; iSection++)
            {
                VisitSwitchSection(switchSections[iSection], iSection == iLastSection);
                // Even though it is illegal for the end of a switch section to be reachable, in erroneous
                // code it may be reachable.  We treat that as an implicit break (branch to afterSwitchState).
                IntersectWith(ref afterSwitchState, ref this.State);
            }

            if (reachableLabels.Contains(node.BreakLabel))
            {
                IntersectWith(ref afterSwitchState, ref initialState);
            }

            ResolveBreaks(afterSwitchState, node.BreakLabel);
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
    }
}
