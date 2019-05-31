// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class NullableWalker
    {
        internal sealed class SnapshotManager
        {
            /// <summary>
            /// The int key corresponds to <see cref="Snapshot.GlobalStateIndex"/>.
            /// </summary>
            private readonly ImmutableArray<SharedWalkerState> _walkerGlobalStates;
            /// <summary>
            /// The dictionary key corresponds to Syntax position.
            /// </summary>
            private readonly ImmutableSortedDictionary<int, Snapshot> _incrementalSnapshots;

            private SnapshotManager(ImmutableArray<SharedWalkerState> walkerGlobalStates, ImmutableSortedDictionary<int, Snapshot> incrementalSnapshots)
            {
                _walkerGlobalStates = walkerGlobalStates;
                _incrementalSnapshots = incrementalSnapshots;
            }

            internal NullableWalker RestoreWalkerToAnalyzeNewNode(
                int position,
                BoundNode nodeToAnalyze,
                Binder binder,
                ImmutableDictionary<BoundExpression, (NullabilityInfo, TypeSymbol)>.Builder analyzedNullabilityMap)
            {
                // TODO: Use a more efficient storage and lookup mechanism like an AVL tree here.
                // https://github.com/dotnet/roslyn/issues/35037
                Snapshot incrementalSnapshot = default;
                bool foundSnapshot = false;
                foreach (var (currentPosition, currentSnapshot) in _incrementalSnapshots)
                {
                    if (currentPosition <= position)
                    {
                        incrementalSnapshot = currentSnapshot;
                        foundSnapshot = true;
                        break;
                    }
                }

                if (!foundSnapshot)
                {
                    return null;
                }

                var globalState = _walkerGlobalStates[incrementalSnapshot.GlobalStateIndex];
                var variableState = new VariableState(globalState.VariableSlot, globalState.VariableBySlot, globalState.VariableTypes, incrementalSnapshot.VariableState.Clone());
                var method = globalState.Symbol as MethodSymbol;
                return new NullableWalker(
                    binder.Compilation,
                    globalState.Symbol,
                    useMethodSignatureParameterTypes: !(method is null),
                    method,
                    nodeToAnalyze,
                    binder,
                    binder.Conversions,
                    variableState,
                    returnTypesOpt: null,
                    analyzedNullabilityMap,
                    snapshotBuilderOpt: null);
            }

#if DEBUG
            internal void VerifyNode(BoundNode node)
            {
                if (node.Kind == BoundKind.TypeExpression || node.WasCompilerGenerated)
                {
                    return;
                }

                Debug.Assert(_incrementalSnapshots.ContainsKey(node.Syntax.SpanStart), $"Did not find a snapshot for {node} `{node.Syntax}.`");
                Debug.Assert(_walkerGlobalStates.Length > _incrementalSnapshots[node.Syntax.SpanStart].GlobalStateIndex, $"Did not find global state for {node} `{node.Syntax}`.");
            }
#endif

            internal sealed class Builder
            {
                private readonly ArrayBuilder<SharedWalkerState> _walkerStates = ArrayBuilder<SharedWalkerState>.GetInstance();
                /// <summary>
                /// Snapshots are kept in a dictionary of position -> snapshot at that position.
                /// </summary>
                private readonly ImmutableSortedDictionary<int, Snapshot>.Builder _incrementalSnapshots = ImmutableSortedDictionary.CreateBuilder<int, Snapshot>();
                /// <summary>
                /// Every walker is walking a specific symbol, and can potentially walk each symbol multiple times
                /// to get to a stable state. Each of these symbols gets a single global state slot, which this
                /// dictionary keeps track of. These slots correspond to indexes into <see cref="_walkerStates"/>.
                /// </summary>
                private readonly PooledDictionary<Symbol, int> _symbolToSlot = PooledDictionary<Symbol, int>.GetInstance();
                private int _currentWalkerSlot = -1;
                private Symbol _initialSymbol = null;
#if DEBUG
                private Symbol _currentSymbol = null;
#endif

                internal SnapshotManager ToManagerAndFree()
                {
                    Debug.Assert(_currentWalkerSlot == -1, "Attempting to finalize snapshots before all walks completed");
                    Debug.Assert(_symbolToSlot.Count == _walkerStates.Count);
                    _symbolToSlot.Free();
                    return new SnapshotManager(
                        _walkerStates.ToImmutableAndFree(),
                        _incrementalSnapshots.ToImmutable());
                }

                internal void EnterNewWalker(Symbol symbol)
                {
#if DEBUG
                    Debug.Assert(symbol is object);
                    Debug.Assert((_currentSymbol is null && _currentWalkerSlot == -1 && _initialSymbol is null) ||
                                 symbol.ContainingSymbol.Equals(_currentSymbol));
                    _currentSymbol = symbol;
#endif
                    _initialSymbol ??= symbol;
                    var previousSlot = _currentWalkerSlot;

                    // Because we potentially run multiple passes, we
                    // need to make sure we use the same final shared
                    // state for following passes.
                    if (_symbolToSlot.TryGetValue(symbol, out var slot))
                    {
                        _currentWalkerSlot = slot;
                    }
                    else
                    {
                        _currentWalkerSlot = _symbolToSlot.Count;
                        _symbolToSlot.Add(symbol, _currentWalkerSlot);
                    }
                }

                internal void ExitWalker(SharedWalkerState stableState, Symbol symbol)
                {
#if DEBUG
                    Debug.Assert(_currentSymbol.Equals(symbol));
                    _currentSymbol = symbol.ContainingSymbol;
#endif
                    _walkerStates.SetItem(_currentWalkerSlot, stableState);
                    if (_initialSymbol.Equals(symbol))
                    {
                        Debug.Assert(_currentWalkerSlot == 0);
                        _currentWalkerSlot = -1;
                    }
                    else
                    {
                        _currentWalkerSlot = _symbolToSlot[symbol.ContainingSymbol];
                    }
                }

                internal void TakeIncrementalSnapshot(BoundNode node, LocalState currentState)
                {
                    if (node == null || node.WasCompilerGenerated)
                    {
                        return;
                    }

                    // Note that we can't use Add here, as this is potentially not the stable
                    // state of this node and we could get updated states later.
                    _incrementalSnapshots[node.Syntax.SpanStart] = new Snapshot(currentState.Clone(), _currentWalkerSlot);
                }
            }
        }

        /// <summary>
        /// Contains the shared state state used to restore the walker at a specific point
        /// </summary>
        internal struct SharedWalkerState
        {
            internal readonly ImmutableDictionary<VariableIdentifier, int> VariableSlot;
            internal readonly ImmutableArray<VariableIdentifier> VariableBySlot;
            internal readonly ImmutableDictionary<Symbol, TypeWithAnnotations> VariableTypes;
            internal readonly Symbol Symbol;

            internal SharedWalkerState(
                ImmutableDictionary<VariableIdentifier, int> variableSlot,
                ImmutableArray<VariableIdentifier> variableBySlot,
                ImmutableDictionary<Symbol, TypeWithAnnotations> variableTypes,
                Symbol symbol)
            {
                VariableSlot = variableSlot;
                VariableBySlot = variableBySlot;
                VariableTypes = variableTypes;
                Symbol = symbol;
            }
        }

        /// <summary>
        /// Contains a snapshot of the state of the NullableWalker at any given point of execution, used for restoring the walker to
        /// a specific point for speculatively analyzing a piece of code that does not appear in the original tree.
        /// </summary>
        private readonly struct Snapshot
        {
            internal readonly LocalState VariableState;
            internal readonly int GlobalStateIndex;

            internal Snapshot(LocalState variableState, int globalStateIndex)
            {
                VariableState = variableState;
                GlobalStateIndex = globalStateIndex;
            }
        }
    }
}
