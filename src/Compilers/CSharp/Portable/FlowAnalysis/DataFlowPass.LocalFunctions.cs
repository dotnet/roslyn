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

        /// <summary>
        /// True if we're currently recording reads, rather than reporting
        /// diagnostics.
        /// </summary>
        private bool _recordingReads = false;

        private SmallDictionary<LocalFunctionSymbol, LocalFuncInfo> _readVars =
            new SmallDictionary<LocalFunctionSymbol, LocalFuncInfo>();


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


            var recordingState = _recordingReads;
            _recordingReads = true;

            do
            {
                ClearDirtyBits(_readVars.Values);

                foreach (var stmt in block.Statements)
                {
                    if (stmt.Kind == BoundKind.LocalFunctionStatement)
                    {
                        var savedState = this.State;
                        var localFunc = (BoundLocalFunctionStatement)stmt;

                        // Clear the state before visitation since all captured
                        // reads could be unassigned at the local function callsite
                        this.State = new LocalState(
                            BitVector.Create(this.State.Assigned.Capacity));

                        // Assign all parameters of all parent functions
                        EnterAllParameters();

                        VisitLambdaOrLocalFunction(localFunc,
                            recordAssigns: false);

                        this.State = savedState;
                    }
                }
            } while (HasDirtyLocalFunctions(_readVars.Values));

            _recordingReads = recordingState;
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

            LocalFuncInfo readInfo;
            if (_readVars.TryGetValue(localFunc, out readInfo))
            {
                if (!readInfo.UsedVars.Contains(slot))
                {
                    readInfo.IsDirty = true;
                    readInfo.UsedVars.Add(slot);
                }
            }
            else
            {
                readInfo = new LocalFuncInfo();
                readInfo.IsDirty = true;
                readInfo.UsedVars.Add(slot);
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
            LocalFuncInfo info;
            if (_readVars.TryGetValue(localFunc, out info))
            {
                foreach (var usedVarSlot in info.UsedVars)
                {
                    var symbol = variableBySlot[usedVarSlot].Symbol;
                    CheckAssigned(symbol, syntax, usedVarSlot);
                }
            }

            // Now the writes
            if (writes &&
                _localFuncAssignmentResults?.AssignedVars
                    .TryGetValue(localFunc, out info) == true)
            {
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

        protected bool IsCapturedInLocalFunction(int slot,
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
