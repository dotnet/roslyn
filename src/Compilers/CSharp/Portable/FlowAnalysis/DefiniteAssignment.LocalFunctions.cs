// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DefiniteAssignmentPass
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
                Meet(ref this.State, ref usages.WrittenVars);
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
                    CheckIfAssignedDuringLocalFunctionReplay(symbol, syntax, slot);
                }
            }
        }

        /// <summary>
        /// Check that the given variable is definitely assigned when replaying local function
        /// reads. If not, produce an error.
        /// </summary>
        /// <remarks>
        /// Specifying the slot manually may be necessary if the symbol is a field,
        /// in which case <see cref="LocalDataFlowPass{TLocalState}.VariableSlot(Symbol, int)"/>
        /// will not know which containing slot to look for.
        /// </remarks>
        private void CheckIfAssignedDuringLocalFunctionReplay(Symbol symbol, SyntaxNode node, int slot)
        {
            Debug.Assert(!IsConditionalState);
            if ((object)symbol != null)
            {
                NoteRead(symbol);

                if (this.State.Reachable)
                {
                    if (slot >= this.State.Assigned.Capacity)
                    {
                        Normalize(ref this.State);
                    }

                    if (slot > 0 && !this.State.IsAssigned(slot))
                    {
                        // Local functions can "call forward" to after a variable has
                        // been declared but before it has been assigned, so we can never
                        // consider the declaration location when reporting errors.
                        ReportUnassignedIfNotCapturedInLocalFunction(symbol, node, slot, skipIfUseBeforeDeclaration: false);
                    }
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
            var oldSymbol = this.currentSymbol;
            var localFuncSymbol = localFunc.Symbol;
            this.currentSymbol = localFuncSymbol;

            var oldPending = SavePending(); // we do not support branches into a lambda

            // SPEC: The entry point to a local function is always reachable.
            // Captured variables are definitely assigned if they are definitely assigned on
            // all branches into the local function.

            var savedState = this.State;
            this.State = this.TopState();

            if (!localFunc.WasCompilerGenerated) EnterParameters(localFuncSymbol.Parameters);

            // Captured variables are definitely assigned if they are assigned on
            // all branches into the local function, so we store all reads from
            // possibly unassigned captured variables and later report definite
            // assignment errors if any of the captured variables is not assigned
            // on a particular branch.
            //
            // Assignments to captured variables are also recorded, as calls to local functions
            // definitely assign captured variables if the variables are definitely assigned at
            // all branches out of the local function.

            var usages = GetOrCreateLocalFuncUsages(localFuncSymbol);
            var oldReads = usages.ReadVars.Clone();
            usages.ReadVars.Clear();

            var oldPending2 = SavePending();

            // If this is an iterator, there's an implicit branch before the first statement
            // of the function where the enumerable is returned.
            if (localFuncSymbol.IsIterator)
            {
                PendingBranches.Add(new PendingBranch(null, this.State, null));
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

            // Intersect the state of all branches out of the local function
            LocalState stateAtReturn = this.State;
            foreach (PendingBranch pending in pendingReturns)
            {
                this.State = pending.State;
                BoundNode branch = pending.Branch;

                // Pass the local function identifier as a location if the branch
                // is null or compiler generated.
                LeaveParameters(localFuncSymbol.Parameters,
                  branch?.Syntax,
                  branch?.WasCompilerGenerated == false ? null : location);

                Join(ref stateAtReturn, ref this.State);
            }

            // Check for changes to the possibly unassigned and assigned sets
            // of captured variables
            if (RecordChangedVars(ref usages.WrittenVars,
                                  ref stateAtReturn,
                                  ref oldReads,
                                  ref usages.ReadVars) &&
                usages.LocalFuncVisited)
            {
                // If the sets have changed and we already used the results
                // of this local function in another computation, the previous
                // calculations may be invalid. We need to analyze until we
                // reach a fixed-point. The previous writes are always valid,
                // so they are stored in usages.WrittenVars, while the reads
                // may be invalidated by new writes, so we throw the results out.
                stateChangedAfterUse = true;
                usages.LocalFuncVisited = false;
            }

            this.State = savedState;
            this.currentSymbol = oldSymbol;

            return null;
        }

        private void RecordReadInLocalFunction(int slot)
        {
            var localFunc = GetNearestLocalFunctionOpt(currentSymbol);

            Debug.Assert(localFunc != null);

            var usages = GetOrCreateLocalFuncUsages(localFunc);

            // If this slot is a struct with individually assignable
            // fields we need to record each field assignment separately,
            // since some fields may be assigned when this read is replayed
            VariableIdentifier id = variableBySlot[slot];
            var type = id.Symbol.GetTypeOrReturnType().Type;

            Debug.Assert(!_emptyStructTypeCache.IsEmptyStructType(type));

            if (EmptyStructTypeCache.IsTrackableStructType(type))
            {
                foreach (var field in _emptyStructTypeCache.GetStructInstanceFields(type))
                {
                    int fieldSlot = GetOrCreateSlot(field, slot);
                    if (fieldSlot > 0 && !State.IsAssigned(fieldSlot))
                    {
                        RecordReadInLocalFunction(fieldSlot);
                    }
                }
            }
            else
            {
                usages.ReadVars[slot] = true;
            }
        }

        private bool RecordChangedVars(ref LocalState oldWrites,
                                       ref LocalState newWrites,
                                       ref BitVector oldReads,
                                       ref BitVector newReads)
        {
            bool anyChanged = Join(ref oldWrites, ref newWrites);

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

        private bool IsCapturedInLocalFunction(int slot)
        {
            if (slot <= 0) return false;

            // Find the root slot, since that would be the only
            // slot, if any, that is captured in a local function
            var rootVarInfo = variableBySlot[RootSlot(slot)];

            var rootSymbol = rootVarInfo.Symbol;

            // A variable is captured in a local function iff its
            // container is higher in the tree than the nearest
            // local function
            var nearestLocalFunc = GetNearestLocalFunctionOpt(currentSymbol);

            return !(nearestLocalFunc is null) && Symbol.IsCaptured(rootSymbol, nearestLocalFunc);
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
