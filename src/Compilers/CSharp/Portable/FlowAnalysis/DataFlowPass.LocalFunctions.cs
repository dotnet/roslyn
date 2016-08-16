// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DataFlowPass
    {
        /// <summary>
        /// Holds info from the assignment pass done by
        /// <see cref="LocalFunctionAssignmentPass"/>.
        /// </summary>
        private LocalFuncAssignmentResults _localFuncAssignmentResults;

        private SmallDictionary<LocalFunctionSymbol, LocalFuncReads> _readVars =
            new SmallDictionary<LocalFunctionSymbol, LocalFuncReads>();


        private class LocalFuncReads
        {
            public BitVector ReadVars = BitVector.Empty;

            public bool Used { get; set; } = false;
        }

        protected class LocalFuncInfo
        {
            public bool IsDirty { get; set; }
            public PooledHashSet<int> UsedVars =
                PooledHashSet<int>.GetInstance();

            public void Free()
            {
                UsedVars.Free();
            }
        }

        protected class LocalFuncAssignmentResults
        {
            public SmallDictionary<LocalFunctionSymbol, LocalFuncInfo> AssignedVars { get; }
            public ImmutableArray<VariableIdentifier> VariableSlots { get; }

            public LocalFuncAssignmentResults(
                SmallDictionary<LocalFunctionSymbol, LocalFuncInfo> assignedVars,
                ImmutableArray<VariableIdentifier> variableSlots)
            {
                AssignedVars = assignedVars;
                VariableSlots = variableSlots;
            }

            public void Free()
            {
                FreeLocalFuncInfos(AssignedVars.Values);
            }
        }

        private static void FreeLocalFuncInfos<T>(T infos)
            where T : IEnumerable<LocalFuncInfo>
        {
            foreach (var info in infos)
            {
                info.Free();
            }
        }

        protected static bool HasDirtyLocalFunctions<T>(T localFuncInfos)
            where T : IEnumerable<LocalFuncInfo>
        {
            foreach (var info in localFuncInfos)
            {
                if (info.IsDirty)
                {
                    return true;
                }
            }
            return false;
        }

        protected static void ClearDirtyBits<T>(T localFuncInfos)
            where T : IEnumerable<LocalFuncInfo>
        {
            foreach (var info in localFuncInfos)
            {
                info.IsDirty = false;
            }
        }

        /// <summary>
        /// The results from running the assignment pass.
        /// </summary>
        protected virtual LocalFuncAssignmentResults GetResults() => null;

        /// <summary>
        /// Records all possibly-unassigned reads in all local
        /// functions in the given block.
        /// </summary>
        private void RecordReadsInLocalFunctions(BoundBlock block)
        {
            if (block.LocalFunctions.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (var stmt in block.Statements)
            {
                if (stmt.Kind == BoundKind.LocalFunctionStatement)
                {
                    var localFunc = (BoundLocalFunctionStatement)stmt;
                    VisitLocalFunction(localFunc, recordAssigns: false);
                }
            }
        }

        private void EnterAllParameters()
        {
            foreach (var info in variableBySlot)
            {
                if (info.Symbol?.Kind == SymbolKind.Parameter)
                {
                    EnterParameter((ParameterSymbol)info.Symbol);
                }
            }
        }

        private void RecordReadInLocalFunction(int slot)
        {
            var localFunc = GetNearestLocalFunctionOpt(currentMethodOrLambda);

            Debug.Assert(localFunc != null);

            LocalFuncReads readInfo;
            if (_readVars.TryGetValue(localFunc, out readInfo))
            {
                if (!readInfo.ReadVars[slot])
                {
                    readInfo.ReadVars[slot] = true;

                    // If we've already "used" the reads of this local
                    // function then we know the previous use is invalid --
                    // it didn't include the read that was added here. We
                    // must do another pass to include this read.
                    if (readInfo.Used)
                    {
                        stateChangedAfterUse = true;
                        readInfo.Used = false;
                    }
                }
            }
            else
            {
                readInfo = new LocalFuncReads();
                readInfo.ReadVars[slot] = true;
                _readVars.Add(localFunc, readInfo);
            }
        }

        /// <summary>
        /// At the local function's use site, checks that all variables read
        /// are assigned and assigns all variables that are definitely assigned
        /// to be definitely assigned.
        /// </summary>
        protected virtual void ReplayReadsAndWrites(
            LocalFunctionSymbol localFunc,
            CSharpSyntaxNode syntax,
            bool writes)
        {
            _usedLocalFunctions.Add(localFunc);

            // First process the reads
            LocalFuncReads reads;
            if (_readVars.TryGetValue(localFunc, out reads))
            {
                // Start at slot 1 (slot 0 just indicates reachability)
                for (int slot = 1; slot < reads.ReadVars.Capacity; slot++)
                {
                    if (reads.ReadVars[slot])
                    {
                        var symbol = variableBySlot[slot].Symbol;
                        CheckAssigned(symbol, syntax, slot);
                    }
                }
            }
            else
            {
                // No reads to replay, but we need to record that we used
                // the reads, in case reads are later added to the read set
                reads = new LocalFuncReads();
                reads.Used = true;
                _readVars.Add(localFunc, reads);
            }

            // Now the writes
            LocalFuncInfo info = null;
            if (writes &&
                _localFuncAssignmentResults?.AssignedVars
                    .TryGetValue(localFunc, out info) == true)
            {
                Debug.Assert(info != null);

                foreach (var usedVarSlot in info.UsedVars)
                {
                    SetSlotAssigned(usedVarSlot);
                }
            }
        }

        protected int RootSlot(int slot)
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

        protected BoundNode VisitLocalFunction(BoundLocalFunctionStatement localFunc, bool recordAssigns)
        {
            var oldMethodOrLambda = this.currentMethodOrLambda;
            this.currentMethodOrLambda = localFunc.Symbol;

            var oldPending = SavePending(); // we do not support branches into a lambda

            // Local functions don't affect outer state and are analyzed
            // with everything unassigned and reachable
            var savedState = this.State;
            this.State = this.ReachableState();

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
                }
                else
                {
                    // other ways of branching out of a lambda are errors, previously reported in control-flow analysis
                }

                IntersectWith(ref stateAtReturn, ref this.State);
            }

            if (recordAssigns)
            {
                // Check for assignments to captured variables
                RecordCapturedAssigns(ref stateAtReturn);
            }

            this.State = savedState;
            this.currentMethodOrLambda = oldMethodOrLambda;

            return null;
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
