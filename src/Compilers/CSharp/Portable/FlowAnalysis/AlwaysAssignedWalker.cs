// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A region analysis walker that computes the set of variables that are always assigned a value
    /// in the region. A variable is "always assigned" in a region if an analysis of the region that
    /// starts with the variable unassigned ends with the variable assigned.
    /// </summary>
    internal class AlwaysAssignedWalker : AbstractRegionDataFlowPass
    {
        private LocalState _endOfRegionState;
        private readonly HashSet<LabelSymbol> _labelsInside = new HashSet<LabelSymbol>();

        private AlwaysAssignedWalker(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion)
            : base(compilation, member, node, firstInRegion, lastInRegion)
        {
        }

        internal static IEnumerable<Symbol> Analyze(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion)
        {
            var walker = new AlwaysAssignedWalker(compilation, member, node, firstInRegion, lastInRegion);
            bool badRegion = false;
            try
            {
                var result = walker.Analyze(ref badRegion);
                return badRegion ? SpecializedCollections.EmptyEnumerable<Symbol>() : result;
            }
            finally
            {
                walker.Free();
            }
        }

        private new List<Symbol> Analyze(ref bool badRegion)
        {
            base.Analyze(ref badRegion, null);
            List<Symbol> result = new List<Symbol>();
            Debug.Assert(!IsInside);
            if (_endOfRegionState.Reachable)
            {
                foreach (var i in _endOfRegionState.Assigned.TrueBits())
                {
                    if (i >= variableBySlot.Length)
                    {
                        continue;
                    }

                    var v = base.variableBySlot[i];
                    if (v.Exists && !(v.Symbol is FieldSymbol))
                    {
                        result.Add(v.Symbol);
                    }
                }
            }

            return result;
        }

        protected override void WriteArgument(BoundExpression arg, RefKind refKind, MethodSymbol method, ParameterSymbol parameter)
        {
            // ref parameter does not "always" assign.
            if (refKind == RefKind.Out)
            {
                Assign(arg, value: null, valueIsNotNull: null);
            }
        }

        protected override void ResolveBranch(PendingBranch pending, LabelSymbol label, BoundStatement target, ref bool labelStateChanged)
        {
            // branches into a region are considered entry points
            if (IsInside && pending.Branch != null && !RegionContains(pending.Branch.Syntax.Span))
            {
                pending.State = pending.State.Reachable ? ReachableState() : UnreachableState();
            }

            base.ResolveBranch(pending, label, target, ref labelStateChanged);
        }

        public override BoundNode VisitLabel(BoundLabel node)
        {
            ResolveLabel(node, node.Label);
            return base.VisitLabel(node);
        }

        public override BoundNode VisitLabeledStatement(BoundLabeledStatement node)
        {
            ResolveLabel(node, node.Label);
            return base.VisitLabeledStatement(node);
        }

        private void ResolveLabel(BoundNode node, LabelSymbol label)
        {
            if (node.Syntax != null && RegionContains(node.Syntax.Span)) _labelsInside.Add(label);
        }

        protected override void EnterRegion()
        {
            this.State = ReachableState();
            base.EnterRegion();
        }

        protected override void LeaveRegion()
        {
            if (this.IsConditionalState)
            {
                // If the region is in a condition, then the state will be split and state.Assigned will
                // be null.  Merge to get sensible results.
                _endOfRegionState = StateWhenTrue.Clone();
                IntersectWith(ref _endOfRegionState, ref StateWhenFalse);
            }
            else
            {
                _endOfRegionState = this.State.Clone();
            }

            foreach (var branch in base.PendingBranches)
            {
                if (branch.Branch != null && RegionContains(branch.Branch.Syntax.Span) && !_labelsInside.Contains(branch.Label))
                {
                    IntersectWith(ref _endOfRegionState, ref branch.State);
                }
            }

            base.LeaveRegion();
        }
    }
}
