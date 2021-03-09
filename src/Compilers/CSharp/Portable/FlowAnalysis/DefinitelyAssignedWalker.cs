// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if DEBUG
// See comment in DefiniteAssignment.
#define REFERENCE_STATE
#endif

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A region analysis walker that computes the set of variables that are definitely assigned
    /// when a region is entered or exited.
    /// </summary>
    internal sealed class DefinitelyAssignedWalker : AbstractRegionDataFlowPass
    {
        private readonly HashSet<Symbol> _definitelyAssignedOnEntry = new HashSet<Symbol>();
        private readonly HashSet<Symbol> _definitelyAssignedOnExit = new HashSet<Symbol>();

        private DefinitelyAssignedWalker(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            BoundNode firstInRegion,
            BoundNode lastInRegion)
            : base(compilation, member, node, firstInRegion, lastInRegion)
        {
        }

        internal static (HashSet<Symbol> entry, HashSet<Symbol> exit) Analyze(
            CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion)
        {
            var walker = new DefinitelyAssignedWalker(compilation, member, node, firstInRegion, lastInRegion);
            try
            {
                bool badRegion = false;
                walker.Analyze(ref badRegion, diagnostics: null);
                return badRegion
                    ? (new HashSet<Symbol>(), new HashSet<Symbol>())
                    : (walker._definitelyAssignedOnEntry, walker._definitelyAssignedOnExit);
            }
            finally
            {
                walker.Free();
            }
        }

        protected override void EnterRegion()
        {
            ProcessRegion(_definitelyAssignedOnEntry);
            base.EnterRegion();
        }

        protected override void LeaveRegion()
        {
            ProcessRegion(_definitelyAssignedOnExit);
            base.LeaveRegion();
        }

        private void ProcessRegion(HashSet<Symbol> definitelyAssigned)
        {
            // this can happen multiple times as flow analysis is multi-pass.  Always 
            // take the latest data and use that to determine our result.
            definitelyAssigned.Clear();

            if (this.IsConditionalState)
            {
                // We're in a state where there are different flow paths (i.e. when-true and when-false).
                // In that case, a variable is only definitely assigned if it's definitely assigned through
                // both paths.
                this.ProcessState(definitelyAssigned, this.StateWhenTrue, this.StateWhenFalse);
            }
            else
            {
                this.ProcessState(definitelyAssigned, this.State, state2opt: null);
            }
        }

#if REFERENCE_STATE
        private void ProcessState(HashSet<Symbol> definitelyAssigned, LocalState state1, LocalState state2opt)
#else
        private void ProcessState(HashSet<Symbol> definitelyAssigned, LocalState state1, LocalState? state2opt)
#endif
        {
            foreach (var slot in state1.Assigned.TrueBits())
            {
                if (slot < variableBySlot.Count &&
                    state2opt?.IsAssigned(slot) != false &&
                    variableBySlot[slot].Symbol is { } symbol &&
                    symbol.Kind != SymbolKind.Field)
                {
                    definitelyAssigned.Add(symbol);
                }
            }
        }
    }
}
