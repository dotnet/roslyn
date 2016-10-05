// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DataFlowPass
    {
        private readonly SmallDictionary<LocalFunctionSymbol, LocalFuncUsages> _localFuncVarUsages =
            new SmallDictionary<LocalFunctionSymbol, LocalFuncUsages>();

        private class LocalFuncUsages
        {
            public BitVector ReadVars = BitVector.Empty;
            public LocalState WrittenVars;

            public LocalFuncUsages(LocalState unreachableState)
            {
                // If we have yet to analyze the local function
                // definition, assume it definitely assigns everything
                WrittenVars = unreachableState;
            }

            public bool LocalFuncVisited { get; set; } = false;
        }

        /// <summary>
        /// At the local function's use site, checks that all variables read
        /// are assigned and assigns all variables that are definitely assigned
        /// to be definitely assigned.
        /// </summary>
        private void ReplayReadsAndWrites(LocalFunctionSymbol localFunc,
                                          SyntaxNode syntax,
                                          bool writes)
        {
            _usedLocalFunctions.Add(localFunc);

            var usages = GetOrCreateLocalFuncUsages(localFunc);

            // First process the reads
            ReplayReads(ref usages.ReadVars, syntax);

            // Now the writes
            if (writes)
            {
                UnionWith(ref this.State, ref usages.WrittenVars);
            }

            usages.LocalFuncVisited = true;
        }

        private void ReplayReads(ref BitVector reads, SyntaxNode syntax)
        {
            // Start at slot 1 (slot 0 just indicates reachability)
            for (int slot = 1; slot < reads.Capacity; slot++)
            {
                if (reads[slot])
                {
                    var symbol = variableBySlot[slot].Symbol;
                    CheckAssigned(symbol, syntax, slot);
                }
            }
        }

        private int RootSlot(int slot)
        {
            while (true)
            {
                var varInfo = variableBySlot[slot];
                if (varInfo.ContainingSlot == 0)
                {
                    return slot;
                }
                else
                {
                    slot = varInfo.ContainingSlot;
                }
            }
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement localFunc)
        {
            var oldMethodOrLambda = this.currentMethodOrLambda;
            var localFuncSymbol = localFunc.Symbol;
            this.currentMethodOrLambda = localFuncSymbol;

            var oldPending = SavePending(); // we do not support branches into a lambda

            // Local functions don't affect outer state and are analyzed
            // with everything unassigned and reachable
            var savedState = this.State;
            this.State = this.ReachableState();

            var usages = GetOrCreateLocalFuncUsages(localFuncSymbol);
            var oldReads = usages.ReadVars;
            usages.ReadVars = BitVector.Empty;

            if (!localFunc.WasCompilerGenerated) EnterParameters(localFuncSymbol.Parameters);

            var oldPending2 = SavePending();

            // If this is an iterator, there's an implicit branch before the first statement
            // of the function where the enumerable is returned.
            if (localFuncSymbol.IsIterator)
            {
                PendingBranches.Add(new PendingBranch(null, this.State));
            }

            VisitAlways(localFunc.Body);
            RestorePending(oldPending2); // process any forward branches within the lambda body
            ImmutableArray<PendingBranch> pendingReturns = RemoveReturns();
            RestorePending(oldPending);

            Location location = null;

            if (!localFuncSymbol.Locations.IsDefaultOrEmpty)
            {
                location = localFuncSymbol.Locations[0];
            }

            LeaveParameters(localFuncSymbol.Parameters, localFunc.Syntax, location);

            LocalState stateAtReturn = this.State;
            foreach (PendingBranch pending in pendingReturns)
            {
                this.State = pending.State;
                BoundNode branch = pending.Branch;
                LeaveParameters(localFuncSymbol.Parameters, branch?.Syntax,
                                branch?.WasCompilerGenerated == true
                                    ? location : null);
                IntersectWith(ref stateAtReturn, ref this.State);
            }

            // Check for changes to the read and write sets
            if (RecordChangedVars(ref usages.WrittenVars,
                                  ref stateAtReturn,
                                  ref oldReads,
                                  ref usages.ReadVars) &&
                usages.LocalFuncVisited)
            {
                stateChangedAfterUse = true;
                usages.LocalFuncVisited = false;
            }

            this.State = savedState;
            this.currentMethodOrLambda = oldMethodOrLambda;

            return null;
        }

        private void RecordReadInLocalFunction(int slot)
        {
            var localFunc = GetNearestLocalFunctionOpt(currentMethodOrLambda);

            Debug.Assert(localFunc != null);

            var usages = GetOrCreateLocalFuncUsages(localFunc);
            usages.ReadVars[slot] = true;
        }

        private bool RecordChangedVars(ref LocalState oldWrites,
                                       ref LocalState newWrites,
                                       ref BitVector oldReads,
                                       ref BitVector newReads)
        {
            bool anyChanged = IntersectWith(ref oldWrites, ref newWrites);

            anyChanged |= RecordCapturedChanges(ref oldReads, ref newReads);

            return anyChanged;
        }

        private bool RecordCapturedChanges(ref BitVector oldState,
                                           ref BitVector newState)
        {
            // Build a list of variables that are both captured and assigned
            var capturedMask = GetCapturedBitmask(ref newState);
            var capturedAndSet = newState;
            capturedAndSet.IntersectWith(capturedMask);

            // Union and check to see if there are any changes
            return oldState.UnionWith(capturedAndSet);
        }

        private BitVector GetCapturedBitmask(ref BitVector state)
        {
            BitVector mask = BitVector.Empty;
            for (int slot = 1; slot < state.Capacity; slot++)
            {
                if (IsCapturedInLocalFunction(slot))
                {
                    mask[slot] = true;
                }
            }

            return mask;
        }

        private bool IsCapturedInLocalFunction(int slot,
            ParameterSymbol rangeVariableUnderlyingParameter = null)
        {
            if (slot <= 0) return false;

            // Find the root slot, since that would be the only
            // slot, if any, that is captured in a local function
            var rootVarInfo = variableBySlot[RootSlot(slot)];

            var rootSymbol = rootVarInfo.Symbol;

            // A variable is captured in a local function iff its
            // container is higher in the tree than the nearest
            // local function
            var nearestLocalFunc = GetNearestLocalFunctionOpt(currentMethodOrLambda);
            return (object)nearestLocalFunc != null &&
                   IsCaptured(rootSymbol, nearestLocalFunc, rangeVariableUnderlyingParameter);
        }

        private LocalFuncUsages GetOrCreateLocalFuncUsages(LocalFunctionSymbol localFunc)
        {
            LocalFuncUsages usages;
            if (!_localFuncVarUsages.TryGetValue(localFunc, out usages))
            {
                usages = _localFuncVarUsages[localFunc] = new LocalFuncUsages(UnreachableState());
            }
            return usages;
        }

        private static LocalFunctionSymbol GetNearestLocalFunctionOpt(Symbol symbol)
        {
            while (symbol != null)
            {
                if (symbol.Kind == SymbolKind.Method &&
                    ((MethodSymbol)symbol).MethodKind == MethodKind.LocalFunction)
                {
                    return (LocalFunctionSymbol)symbol;
                }
                symbol = symbol.ContainingSymbol;
            }
            return null;
        }
    }
}
