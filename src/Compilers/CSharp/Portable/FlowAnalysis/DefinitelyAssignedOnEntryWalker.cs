// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A region analysis walker that computes the set of variables that are definitely assigned
    /// when a region is entered.
    /// </summary>
    internal class DefinitelyAssignedOnEntryWalker : AbstractRegionDataFlowPass
    {
        private readonly HashSet<Symbol> _definitelyAssignedOnEntry = new HashSet<Symbol>();

        private DefinitelyAssignedOnEntryWalker(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            BoundNode firstInRegion,
            BoundNode lastInRegion)
            : base(compilation, member, node, firstInRegion, lastInRegion)
        {
        }

        internal static HashSet<Symbol> Analyze(
            CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion, out bool? succeeded)
        {
            var walker = new DefinitelyAssignedOnEntryWalker(compilation, member, node, firstInRegion, lastInRegion);
            try
            {
                bool badRegion = false;
                var result = walker.Analyze(ref badRegion);
                succeeded = !badRegion;
                return badRegion ? new HashSet<Symbol>() : result;
            }
            finally
            {
                walker.Free();
            }
        }

        private HashSet<Symbol> Analyze(ref bool badRegion)
        {
            base.Analyze(ref badRegion, null);
            return _definitelyAssignedOnEntry;
        }

        protected override void EnterRegion()
        {
            // this can happen multiple times as flow analysis is multi-pass.  Always 
            // take the latest data and use that to determine our result.
            _definitelyAssignedOnEntry.Clear();

            if (this.IsConditionalState)
            {
                // We're in a state where there are different flow paths (i.e. when-true and when-false).
                // In that case, a variable is only definitely assigned if it's definitely assigned through
                // both paths.
                this.ProcessState(this.StateWhenTrue, this.StateWhenFalse);
            }
            else
            {
                this.ProcessState(this.State, state2opt: null);
            }

            base.EnterRegion();
        }

        private void ProcessState(LocalState state1, LocalState state2opt)
        {
            foreach (var slot in state1.Assigned.TrueBits())
            {
                if (slot < variableBySlot.Length &&
                    state2opt?.IsAssigned(slot) != false &&
                    variableBySlot[slot].Symbol is { } symbol &&
                    symbol.Kind != SymbolKind.Field)
                {
                    _definitelyAssignedOnEntry.Add(symbol);
                }
            }
        }
    }
}
