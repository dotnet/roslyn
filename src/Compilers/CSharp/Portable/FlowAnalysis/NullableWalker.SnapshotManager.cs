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
#nullable enable
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

            private readonly ImmutableDictionary<(BoundNode?, Symbol), Symbol> _updatedSymbolsMap;

            private static readonly Func<(int position, Snapshot snapshot), int, int> BinarySearchComparer = (current, target) => current.position.CompareTo(target);

            private SnapshotManager(ImmutableArray<SharedWalkerState> walkerSharedStates, ImmutableArray<(int position, Snapshot snapshot)> incrementalSnapshots, ImmutableDictionary<(BoundNode?, Symbol), Symbol> updatedSymbolsMap)
            {
                _walkerSharedStates = walkerSharedStates;
                _incrementalSnapshots = incrementalSnapshots;
                _updatedSymbolsMap = updatedSymbolsMap;

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
                SnapshotManager.Builder newManagerOpt)
            {
                Snapshot incrementalSnapshot = GetSnapshotForPosition(position);
                var sharedState = _walkerSharedStates[incrementalSnapshot.SharedStateIndex];
                var variableState = new VariableState(sharedState.VariableSlot, sharedState.VariableBySlot, sharedState.VariableTypes, incrementalSnapshot.VariableState.Clone());
                return (new NullableWalker(binder.Compilation,
                                           sharedState.Symbol,
                                           useDelegateInvokeParameterTypes: false,
                                           delegateInvokeMethodOpt: null,
                                           nodeToAnalyze,
                                           binder,
                                           binder.Conversions,
                                           variableState,
                                           returnTypesOpt: null,
                                           analyzedNullabilityMap,
                                           snapshotBuilderOpt: newManagerOpt,
                                           isSpeculative: true),
                        variableState,
                        sharedState.Symbol);
            }

            internal TypeWithAnnotations? GetUpdatedTypeForLocalSymbol(SourceLocalSymbol symbol)
            {
                var snapshot = GetSnapshotForPosition(symbol.IdentifierToken.SpanStart);
                var sharedState = _walkerSharedStates[snapshot.SharedStateIndex];
                if (sharedState.VariableTypes.TryGetValue(symbol, out var updatedType))
                {
                    return updatedType;
                }

                return default;
            }

            internal NamedTypeSymbol? GetUpdatedDelegateTypeForLambda(LambdaSymbol lambda)
            {
                if (_updatedSymbolsMap.TryGetValue((null, lambda), out var updatedDelegate))
                {
                    return (NamedTypeSymbol)updatedDelegate;
                }

                return null;
            }

            internal bool TryGetUpdatedSymbol(BoundNode node, Symbol symbol, out Symbol updatedSymbol)
            {
                return _updatedSymbolsMap.TryGetValue((node, symbol), out updatedSymbol);
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

            internal void VerifyUpdatedSymbols()
            {
                foreach (var ((expr, originalSymbol), updatedSymbol) in _updatedSymbolsMap)
                {
                    var debugText = expr?.Syntax.ToFullString() ?? originalSymbol.ToDisplayString();
                    Debug.Assert((object)originalSymbol != updatedSymbol, $"Recorded exact same symbol for {debugText}");
                    Debug.Assert(originalSymbol is object, $"Recorded null original symbol for {debugText}");
                    Debug.Assert(updatedSymbol is object, $"Recorded null updated symbol for {debugText}");
                    Debug.Assert(AreCloseEnough(originalSymbol, updatedSymbol), @$"Symbol for `{debugText}` changed:
Was {originalSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}
Now {updatedSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
                }
            }
#endif

            internal sealed class Builder
            {
                /// <summary>
                /// Contains the map of expression and original symbol to reinferred symbols, used by the optional
                /// rewriter phase of the compiler.
                /// </summary>
                /// <remarks>
                /// Lambda symbols are mapped to the NameTypeSymbol of the delegate type they were reinferred to,
                /// and are stored with a null node. The LambdaSymbol itself is position-independent, and does not
                /// need any more information to serve as a key.
                /// All other symbol types are stored mapped to exactly the same type as was provided.
                /// </remarks>
                private readonly ImmutableDictionary<(BoundNode?, Symbol), Symbol>.Builder _updatedSymbolMap = ImmutableDictionary.CreateBuilder<(BoundNode?, Symbol), Symbol>(ExpressionAndSymbolEqualityComparer.Instance, SymbolEqualityComparer.ConsiderEverything);

                /// <summary>
                /// Shared walker states are the parts of the walker state that are not unique at a single position,
                /// but are instead used by all snapshots. Each shared state corresponds to one invocation of Analyze,
                /// so entering a lambda or local function will create a new state here. The indexes in this array
                /// correspond to <see cref="Snapshot.SharedStateIndex"/>.
                /// </summary>
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
                    var snapshotsArray = EnumerableExtensions.SelectAsArray<KeyValuePair<int, Snapshot>, (int, Snapshot)>(_incrementalSnapshots, (kvp) => (kvp.Key, kvp.Value));

                    var updatedSymbols = _updatedSymbolMap.ToImmutable();
                    return new SnapshotManager(_walkerStates.ToImmutableAndFree(), snapshotsArray, updatedSymbols);
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

                internal void SetUpdatedSymbol(BoundNode node, Symbol originalSymbol, Symbol updatedSymbol)
                {
#if DEBUG
                    Debug.Assert(AreCloseEnough(originalSymbol, updatedSymbol));
#endif
                    _updatedSymbolMap[GetKey(node, originalSymbol)] = updatedSymbol;
                }

                internal void RemoveSymbolIfPresent(BoundNode node, Symbol symbol)
                {
                    _updatedSymbolMap.Remove(GetKey(node, symbol));
                }

                private static (BoundNode?, Symbol) GetKey(BoundNode node, Symbol symbol)
                {
                    if (node is BoundLambda && symbol is LambdaSymbol)
                    {
                        return (null, symbol);
                    }
                    else
                    {
                        return (node, symbol);
                    }
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
