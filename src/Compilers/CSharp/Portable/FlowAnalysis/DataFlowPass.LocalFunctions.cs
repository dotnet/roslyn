// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
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
            public BitVector WrittenVars = BitVector.Empty;

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

            // First process the reads
            ReplayVarUsage(localFunc,
                           syntax,
                           isWrite: false);

            // Now the writes
            if (writes)
            {
                ReplayVarUsage(localFunc,
                               syntax,
                               isWrite: true);
            }
        }

        private void ReplayVarUsage(LocalFunctionSymbol localFunc,
                                    SyntaxNode syntax,
                                    bool isWrite)
        {
            LocalFuncUsages usages = GetOrCreateLocalFuncUsages(localFunc);
            var state = isWrite ? usages.WrittenVars : usages.ReadVars;

            // Start at slot 1 (slot 0 just indicates reachability)
            for (int slot = 1; slot < state.Capacity; slot++)
            {
                if (state[slot])
                {
                    if (isWrite)
                    {
                        SetSlotAssigned(slot);
                    }
                    else
                    {
                        var symbol = variableBySlot[slot].Symbol;
                        CheckAssigned(symbol, syntax, slot);
                    }
                }
            }

            usages.LocalFuncVisited = true;
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
            this.currentMethodOrLambda = localFunc.Symbol;

            var oldPending = SavePending(); // we do not support branches into a lambda

            // Local functions don't affect outer state and are analyzed
            // with everything unassigned and reachable
            var savedState = this.State;
            this.State = this.ReachableState();

            var usages = GetOrCreateLocalFuncUsages(localFunc.Symbol);
            var oldReads = usages.ReadVars;
            usages.ReadVars = BitVector.Empty;

            if (!localFunc.WasCompilerGenerated) EnterParameters(localFunc.Symbol.Parameters);

            var oldPending2 = SavePending();
            VisitAlways(localFunc.Body);
            RestorePending(oldPending2); // process any forward branches within the lambda body
            ImmutableArray<PendingBranch> pendingReturns = RemoveReturns();
            RestorePending(oldPending);
            LeaveParameters(localFunc.Symbol.Parameters, localFunc.Syntax, null);

            LocalState stateAtReturn = this.State;
            foreach (PendingBranch pending in pendingReturns)
            {
                this.State = pending.State;
                if (pending.Branch.Kind == BoundKind.ReturnStatement)
                {
                    // ensure out parameters are definitely assigned at each return
                    LeaveParameters(localFunc.Symbol.Parameters, pending.Branch.Syntax, null);
                    IntersectWith(ref stateAtReturn, ref this.State);
                }
                else
                {
                    // other ways of branching out of a lambda are errors, previously reported in control-flow analysis
                }
            }

            // Check for changes to the read and write sets
            if (RecordChangedVars(ref usages.WrittenVars,
                                  ref stateAtReturn.Assigned,
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

        private bool RecordChangedVars(ref BitVector oldWrites,
                                       ref BitVector newWrites,
                                       ref BitVector oldReads,
                                       ref BitVector newReads)
        {
            bool anyChanged = RecordCapturedChanges(ref oldWrites, ref newWrites);
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
                usages = new LocalFuncUsages();
                _localFuncVarUsages[localFunc] = usages;
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
