// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DefiniteAssignmentPass
    {
        internal sealed class LocalFunctionState : AbstractLocalFunctionState
        {
            public BitVector ReadVars = BitVector.Empty;
            public ref LocalState WrittenVars => ref StateFromBottom;

            public LocalFunctionState(LocalState unreachableState)
                : base(unreachableState)
            { }
        }

        protected override LocalFunctionState CreateLocalFunctionState() => new LocalFunctionState(UnreachableState());

        /// <summary>
        /// At the local function's use site, checks that all variables read are assigned.
        /// </summary>
        protected override void VisitLocalFunctionUse(LocalFunctionSymbol localFunc, LocalFunctionState localFunctionState, SyntaxNode syntax)
        {
            _usedLocalFunctions.Add(localFunc);

            var reads = localFunctionState.ReadVars;

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
        /// in which case <see cref="LocalDataFlowPass{TLocalState, TLocalFunctionState}.VariableSlot(Symbol, int)"/>
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

        private void RecordReadInLocalFunction(int slot)
        {
            var localFunc = GetNearestLocalFunctionOpt(CurrentSymbol);

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
            var nearestLocalFunc = GetNearestLocalFunctionOpt(CurrentSymbol);

            return !(nearestLocalFunc is null) && Symbol.IsCaptured(rootSymbol, nearestLocalFunc);
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

        protected override LocalFunctionState LocalFunctionStart(LocalFunctionState startState)
        {
            // Captured variables are definitely assigned if they are assigned on
            // all branches into the local function, so we store all reads from
            // possibly unassigned captured variables and later report definite
            // assignment errors if any of the captured variables is not assigned
            // on a particular branch.

            var savedState = new LocalFunctionState(UnreachableState());
            savedState.ReadVars = startState.ReadVars.Clone();
            startState.ReadVars.Clear();
            return savedState;
        }

        /// <summary>
        /// State changes are handled by the base class. We override to find captured variables that
        /// have been read before they were assigned and determine if the set has changed.
        /// </summary>
        protected override bool LocalFunctionEnd(
            LocalFunctionState savedState,
            LocalFunctionState currentState,
            ref LocalState stateAtReturn)
        {
            // Build a list of variables that are both captured and read before assignment
            var capturedMask = GetCapturedBitmask(ref currentState.ReadVars);
            var capturedAndRead = currentState.ReadVars;
            capturedAndRead.IntersectWith(capturedMask);

            // Union and check to see if there are any changes
            return savedState.ReadVars.UnionWith(capturedAndRead);
        }
    }
}
