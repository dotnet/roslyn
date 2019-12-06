// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class AbstractFlowPass<TLocalState, TLocalFunctionState>
    {
        internal abstract class AbstractLocalFunctionState
        {
            /// <summary>
            /// This is the state from the local function which makes the
            /// current state less specific. For example, in nullable analysis
            /// this would be captured variables that may be nullable after
            /// calling the local function. When a local function is called,
            /// this state is <see cref="Join(ref TLocalState, ref TLocalState)"/>
            /// with the current state.
            /// </summary>
            public TLocalState StateFromBottom;

            /// <summary>
            /// This is the part of the local function transfer function which
            /// transfers knowledge additively. For example, in definite
            /// assignment this would be captured state which is assigned by
            /// the local function.  When a local function is called, this
            /// state is <see cref="Meet(ref TLocalState, ref TLocalState)"/>
            /// with the current state.
            /// </summary>
            public TLocalState StateFromTop;

            public AbstractLocalFunctionState(TLocalState unreachableState)
            {
                StateFromBottom = unreachableState.Clone();
                StateFromTop = unreachableState.Clone();
            }

            public bool Visited = false;
        }

        protected abstract TLocalFunctionState CreateLocalFunctionState();

        private SmallDictionary<LocalFunctionSymbol, TLocalFunctionState>? _localFuncVarUsages = null;

        protected TLocalFunctionState GetOrCreateLocalFuncUsages(LocalFunctionSymbol localFunc)
        {
            _localFuncVarUsages ??= new SmallDictionary<LocalFunctionSymbol, TLocalFunctionState>();

            if (!_localFuncVarUsages.TryGetValue(localFunc, out TLocalFunctionState usages))
            {
                usages = CreateLocalFunctionState();
                _localFuncVarUsages[localFunc] = usages;
            }
            return usages;
        }

        public override BoundNode? VisitLocalFunctionStatement(BoundLocalFunctionStatement localFunc)
        {
            var oldSymbol = this.CurrentSymbol;
            var localFuncSymbol = localFunc.Symbol;
            this.CurrentSymbol = localFuncSymbol;

            var oldPending = SavePending(); // we do not support branches into a lambda

            // SPEC: The entry point to a local function is always reachable.
            // Captured variables are definitely assigned if they are definitely assigned on
            // all branches into the local function.

            var savedState = this.State;
            this.State = this.TopState();

            Optional<TLocalState> savedNonMonotonicState = NonMonotonicState;
            if (_nonMonotonicTransfer)
            {
                NonMonotonicState = ReachableBottomState();
            }

            if (!localFunc.WasCompilerGenerated) EnterParameters(localFuncSymbol.Parameters);

            // State changes to captured variables are recorded, as calls to local functions
            // transition the state of captured variables if the variables have state changes
            // across all branches leaving the local function

            var localFunctionState = GetOrCreateLocalFuncUsages(localFuncSymbol);
            var savedLocalFunctionState = LocalFunctionStart(localFunctionState);

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

            Location? location = null;

            if (!localFuncSymbol.Locations.IsDefaultOrEmpty)
            {
                location = localFuncSymbol.Locations[0];
            }

            LeaveParameters(localFuncSymbol.Parameters, localFunc.Syntax, location);

            // Intersect the state of all branches out of the local function
            var stateAtReturn = this.State;
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

            // Record any changes to the state of captured variables
            if (RecordStateChange(
                    savedLocalFunctionState,
                    localFunctionState,
                    ref stateAtReturn) &&
                localFunctionState.Visited)
            {
                // If the sets have changed and we already used the results
                // of this local function in another computation, the previous
                // calculations may be invalid. We need to analyze until we
                // reach a fixed-point. 
                stateChangedAfterUse = true;
                localFunctionState.Visited = false;
            }

            this.State = savedState;
            NonMonotonicState = savedNonMonotonicState;
            this.CurrentSymbol = oldSymbol;

            return null;
        }

        private bool RecordStateChange(
            TLocalFunctionState savedState,
            TLocalFunctionState currentState,
            ref TLocalState stateAtReturn)
        {
            bool anyChanged = Join(ref currentState.StateFromTop, ref stateAtReturn);

            if (NonMonotonicState.HasValue)
            {
                var value = NonMonotonicState.Value;
                // Since only state moving up gets stored in the non-monotonic state,
                // Meet with the stateAtReturn, which records all state changes. If
                // a state moved up, then down, the final state should be down.
                Meet(ref value, ref stateAtReturn);
                anyChanged |= Join(ref currentState.StateFromBottom, ref value);
            }

            // N.B. Do NOT shortcut this operation. LocalFunctionEnd may have important
            // side effects to the local function state
            anyChanged |= LocalFunctionEnd(savedState, currentState, ref stateAtReturn);
            return anyChanged;
        }

        /// <summary>
        /// Executed at the start of visiting a local function body. The <paramref name="state"/>
        /// parameter holds the current state information for the local function being visited. To
        /// save state information across the analysis, return an instance of <typeparamref name="TLocalFunctionState"/>.
        /// </summary>
        protected virtual TLocalFunctionState LocalFunctionStart(TLocalFunctionState state) => state;

        /// <summary>
        /// Executed after visiting a local function body. The <paramref name="savedState"/> is the
        /// return value from <see cref="LocalFunctionStart(TLocalFunctionState)"/>. The <paramref name="currentState"/>
        /// is state information for the local function that was just visited. <paramref name="stateAtReturn"/> is
        /// the state after visiting the method.
        /// </summary>
        protected virtual bool LocalFunctionEnd(
            TLocalFunctionState savedState,
            TLocalFunctionState currentState,
            ref TLocalState stateAtReturn)
        {
            return false;
        }
    }
}
