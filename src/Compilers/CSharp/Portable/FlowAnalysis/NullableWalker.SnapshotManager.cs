// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
            /// The int key corresponds to <see cref="Snapshot.SharedStateIndex"/>.
            /// </summary>
            private readonly ImmutableArray<SharedWalkerState> _walkerSharedStates;

            /// <summary>
            /// The snapshot array should be sorted in ascending order by the position tuple element in order for the binary search algorithm to
            /// function correctly.
            /// </summary>
            private readonly ImmutableArray<(int position, Snapshot snapshot)> _incrementalSnapshots;

            private static readonly Func<(int position, Snapshot snapshot), int, int> BinarySearchComparer = (current, target) => current.position.CompareTo(target);

            private SnapshotManager(ImmutableArray<SharedWalkerState> walkerSharedStates, ImmutableArray<(int position, Snapshot snapshot)> incrementalSnapshots)
            {
                _walkerSharedStates = walkerSharedStates;
                _incrementalSnapshots = incrementalSnapshots;

#if DEBUG
                Debug.Assert(!incrementalSnapshots.IsDefaultOrEmpty);
                int previousPosition = incrementalSnapshots[0].position;
                for (int i = 1; i < incrementalSnapshots.Length; i++)
                {
                    int currentPosition = incrementalSnapshots[i].position;
                    Debug.Assert(currentPosition > previousPosition);
                    previousPosition = currentPosition;
                }
#endif
            }

            internal (NullableWalker, VariableState, Symbol) RestoreWalkerToAnalyzeNewNode(
                int position,
                BoundNode nodeToAnalyze,
                Binder binder,
                ImmutableDictionary<BoundExpression, (NullabilityInfo, TypeSymbol)>.Builder analyzedNullabilityMap,
                ImmutableDictionary<BoundCall, MethodSymbol>.Builder updatedMethodSymbolMap,
                SnapshotManager.Builder newManagerOpt)
            {
                Snapshot incrementalSnapshot = GetSnapshotForPosition(position);
                var sharedState = _walkerSharedStates[incrementalSnapshot.SharedStateIndex];
                var variableState = new VariableState(sharedState.VariableSlot, sharedState.VariableBySlot, sharedState.VariableTypes, incrementalSnapshot.VariableState.Clone());
                var method = sharedState.Symbol as MethodSymbol;
                return (new NullableWalker(binder.Compilation,
                                           sharedState.Symbol,
                                           useMethodSignatureParameterTypes: !(method is null),
                                           method,
                                           nodeToAnalyze,
                                           binder,
                                           binder.Conversions,
                                           variableState,
                                           returnTypesOpt: null,
                                           analyzedNullabilityMap,
                                           updatedMethodSymbolMap,
                                           snapshotBuilderOpt: newManagerOpt,
                                           isSpeculative: true),
                        variableState,
                        sharedState.Symbol);
            }

            internal ImmutableDictionary<Symbol, TypeWithAnnotations> GetVariableTypesForPosition(int position)
            {
                var snapshot = GetSnapshotForPosition(position);
                var sharedState = _walkerSharedStates[snapshot.SharedStateIndex];
                return sharedState.VariableTypes;
            }

            private Snapshot GetSnapshotForPosition(int position)
            {
                var snapshotIndex = _incrementalSnapshots.BinarySearch(position, BinarySearchComparer);

                if (snapshotIndex < 0)
                {
                    // BinarySearch returns the next higher position. Always take the one closest but behind the requested position
                    snapshotIndex = (~snapshotIndex) - 1;

                    // If there was none in the snapshots before the target position, just take index 0
                    if (snapshotIndex < 0) snapshotIndex = 0;
                }

                return _incrementalSnapshots[snapshotIndex].snapshot;
            }

#if DEBUG
            internal void VerifyNode(BoundNode node)
            {
                if (node.Kind == BoundKind.TypeExpression || node.WasCompilerGenerated)
                {
                    return;
                }

                int nodePosition = node.Syntax.SpanStart;
                int position = _incrementalSnapshots.BinarySearch(nodePosition, BinarySearchComparer);

                if (position < 0)
                {
                    Debug.Fail($"Did not find a snapshot for {node} `{node.Syntax}.`");
                }
                Debug.Assert(_walkerSharedStates.Length > _incrementalSnapshots[position].snapshot.SharedStateIndex, $"Did not find shared state for {node} `{node.Syntax}`.");
            }
#endif

            internal sealed class Builder
            {

                private readonly ArrayBuilder<SharedWalkerState> _walkerStates = ArrayBuilder<SharedWalkerState>.GetInstance();
                /// <summary>
                /// Snapshots are kept in a dictionary of position -> snapshot at that position. These are stored in descending order.
                /// </summary>
                private readonly SortedDictionary<int, Snapshot> _incrementalSnapshots = new SortedDictionary<int, Snapshot>();
                /// <summary>
                /// Every walker is walking a specific symbol, and can potentially walk each symbol multiple times
                /// to get to a stable state. Each of these symbols gets a single shared state slot, which this
                /// dictionary keeps track of. These slots correspond to indexes into <see cref="_walkerStates"/>.
                /// </summary>
                private readonly PooledDictionary<Symbol, int> _symbolToSlot = PooledDictionary<Symbol, int>.GetInstance();
                private int _currentWalkerSlot = -1;

                internal SnapshotManager ToManagerAndFree()
                {
                    Debug.Assert(_currentWalkerSlot == -1, "Attempting to finalize snapshots before all walks completed");
                    Debug.Assert(_symbolToSlot.Count == _walkerStates.Count);
                    Debug.Assert(_symbolToSlot.Count > 0);
                    _symbolToSlot.Free();
                    var snapshotsArray = Roslyn.Utilities.EnumerableExtensions.SelectAsArray<KeyValuePair<int, Snapshot>, (int, Snapshot)>(_incrementalSnapshots, (kvp) => (kvp.Key, kvp.Value));

                    return new SnapshotManager(_walkerStates.ToImmutableAndFree(), snapshotsArray);
                }

                internal int EnterNewWalker(Symbol symbol)
                {
                    Debug.Assert(symbol is object);
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

                    return previousSlot;
                }

                internal void ExitWalker(SharedWalkerState stableState, int previousSlot)
                {
                    _walkerStates.SetItem(_currentWalkerSlot, stableState);
                    _currentWalkerSlot = previousSlot;
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
        /// Contains the shared state used to restore the walker at a specific point
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
            internal readonly int SharedStateIndex;

            internal Snapshot(LocalState variableState, int sharedStateIndex)
            {
                VariableState = variableState;
                SharedStateIndex = sharedStateIndex;
            }
        }
    }
}
