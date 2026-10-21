// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DefiniteAssignmentPass
    {
        internal sealed class LocalFunctionState : AbstractLocalFunctionState
        {
            public BitVector ReadVars = BitVector.Empty;
            public BitVector UnsafeAddressOfVars = BitVector.Empty;

            public BitVector CapturedMask = BitVector.Null;
            public BitVector InvertedCapturedMask = BitVector.Null;

            public LocalFunctionState(LocalState stateFromBottom, LocalState stateFromTop)
                : base(stateFromBottom, stateFromTop)
            { }
        }

        protected override LocalFunctionState CreateLocalFunctionState(LocalFunctionSymbol symbol)
            => CreateLocalFunctionState();

        private LocalFunctionState CreateLocalFunctionState()
            => new LocalFunctionState(
                // The bottom state should assume all variables, even new ones, are assigned
                new LocalState(BitVector.AllSet(variableBySlot.Count), normalizeToBottom: true),
                UnreachableState());

        protected override void VisitLocalFunctionUse(
            LocalFunctionSymbol localFunc,
            LocalFunctionState localFunctionState,
            SyntaxNode syntax,
            bool isCall)
        {
            _usedLocalFunctions.Add(localFunc);

            // Check variables that were read before being definitely assigned.
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

            var unsafeAddressOfVars = localFunctionState.UnsafeAddressOfVars;
            for (int slot = 1; slot < unsafeAddressOfVars.Capacity; slot++)
            {
                if (unsafeAddressOfVars[slot])
                {
                    var symbol = variableBySlot[slot].Symbol;
                    CheckIfAssignedDuringLocalFunctionReplay(symbol, syntax, slot, isUnsafeAddressOf: true);
                }
            }

            base.VisitLocalFunctionUse(localFunc, localFunctionState, syntax, isCall);
        }

        /// <summary>
        /// Check that the given variable is definitely assigned when replaying a local function's
        /// requirements, whether from reads or safe-context address-of operations within that function.
        /// </summary>
        /// <remarks>
        /// Specifying the slot manually may be necessary if the symbol is a field,
        /// in which case <see cref="LocalDataFlowPass{TLocalState, TLocalFunctionState}.VariableSlot(Symbol, int)"/>
        /// will not know which containing slot to look for.
        /// </remarks>
        private void CheckIfAssignedDuringLocalFunctionReplay(Symbol symbol, SyntaxNode node, int slot, bool isUnsafeAddressOf = false)
        {
            Debug.Assert(!IsConditionalState);
            if ((object)symbol != null)
            {
                if (!isUnsafeAddressOf)
                {
                    NoteRead(symbol);
                }

                if (this.State.Reachable)
                {
                    if (slot >= this.State.Assigned.Capacity)
                    {
                        Normalize(ref this.State);
                    }

                    if (slot > 0 && !this.State.IsAssigned(slot))
                    {
                        if (isUnsafeAddressOf)
                        {
                            ReportUnsafeUnassignedAddressOf(node, slot);
                            return;
                        }

                        // Local functions can "call forward" to after a variable has
                        // been declared but before it has been assigned, so we can never
                        // consider the declaration location when reporting errors.
                        ReportUnassignedIfNotCapturedInLocalFunction(symbol, node, slot, skipIfUseBeforeDeclaration: false);
                    }
                }
            }
        }

        private void ReportUnsafeUnassignedAddressOf(SyntaxNode syntax, int slot)
        {
            if (IsCapturedInLocalFunction(slot))
            {
                RecordAssignmentRequirementInLocalFunction(slot, isUnsafeAddressOf: true);
                return;
            }

            var location = syntax is PrefixUnaryExpressionSyntax prefix
                ? prefix.OperatorToken.GetLocation()
                : syntax.Location;
            Diagnostics.Add(ErrorCode.ERR_UnsafeUnassignedAddressOf, location);
        }

        private void RecordAssignmentRequirementInLocalFunction(int slot, bool isUnsafeAddressOf = false)
        {
            var localFunc = GetNearestLocalFunctionOpt(CurrentSymbol);

            Debug.Assert(localFunc != null);

            var usages = GetOrCreateLocalFuncUsages(localFunc);

            // If this slot is a struct with individually assignable
            // fields we need to record each field's requirement separately,
            // since some fields may be assigned when the requirement is replayed.
            VariableIdentifier id = variableBySlot[slot];
            var type = id.Symbol.GetTypeOrReturnType().Type;

            Debug.Assert(!_emptyStructTypeCache.IsEmptyStructType(type));

            // Assigning the first element does not fully assign a multi-element inline array.
            if (isUnsafeAddressOf &&
                type.HasInlineArrayAttribute(out int length) && length > 1 &&
                type.TryGetPossiblyUnsupportedByLanguageInlineArrayElementField() is not null)
            {
                usages.UnsafeAddressOfVars[slot] = true;
                return;
            }

            if (EmptyStructTypeCache.IsTrackableStructType(type))
            {
                foreach (var field in _emptyStructTypeCache.GetStructInstanceFields(type))
                {
                    int fieldSlot = GetOrCreateSlot(field, slot);
                    if (fieldSlot > 0 && !State.IsAssigned(fieldSlot))
                    {
                        RecordAssignmentRequirementInLocalFunction(fieldSlot, isUnsafeAddressOf);
                    }
                }
            }
            else
            {
                if (isUnsafeAddressOf)
                {
                    usages.UnsafeAddressOfVars[slot] = true;
                }
                else
                {
                    usages.ReadVars[slot] = true;
                }
            }
        }

        private BitVector GetCapturedBitmask()
        {
            int n = variableBySlot.Count;
            BitVector mask = BitVector.AllSet(n);
            for (int slot = 1; slot < n; slot++)
            {
                mask[slot] = IsCapturedInLocalFunction(slot);
            }

            return mask;
        }

#nullable enable
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
#nullable disable

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

            var savedState = CreateLocalFunctionState();
            savedState.ReadVars = startState.ReadVars.Clone();
            savedState.UnsafeAddressOfVars = startState.UnsafeAddressOfVars.Clone();
            startState.ReadVars.Clear();
            startState.UnsafeAddressOfVars.Clear();
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
            if (currentState.CapturedMask.IsNull)
            {
                currentState.CapturedMask = GetCapturedBitmask();
                currentState.InvertedCapturedMask = currentState.CapturedMask.Clone();
                currentState.InvertedCapturedMask.Invert();
            }
            // Filter the modified state variables to only captured variables
            stateAtReturn.Assigned.IntersectWith(currentState.CapturedMask);
            if (NonMonotonicState.HasValue)
            {
                var state = NonMonotonicState.Value;
                state.Assigned.UnionWith(currentState.InvertedCapturedMask);
                NonMonotonicState = state;
            }

            // Keep requirements only for captured variables.
            var capturedAndRead = currentState.ReadVars;
            capturedAndRead.IntersectWith(currentState.CapturedMask);
            var capturedAndAddressOf = currentState.UnsafeAddressOfVars;
            capturedAndAddressOf.IntersectWith(currentState.CapturedMask);

            // Union and check to see if there are any changes
            bool changed = savedState.ReadVars.UnionWith(capturedAndRead);
            changed |= savedState.UnsafeAddressOfVars.UnionWith(capturedAndAddressOf);
            return changed;
        }
    }
}
