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
        /// <summary>
        /// Maintains a cache of the most recent variable states and manages
        /// taking a snapshot of the NullableWalker in order to be as efficient
        /// as possible when creating many snapshots.
        /// </summary>
        private sealed class Snapshotter
        {
            private readonly NullableWalker _walkerToSnapshot;
            private ImmutableDictionary<VariableIdentifier, int> _lastVariableSlot = ImmutableDictionary<VariableIdentifier, int>.Empty;
            private ImmutableArray<VariableIdentifier> _lastVariableBySlot = ImmutableArray<VariableIdentifier>.Empty;
            private readonly PooledDictionary<BoundNode, Snapshot> _snapshotMap;
            private readonly bool _ownsMap;

            internal Snapshotter(NullableWalker walker, bool takeIncrementalSnapshot)
            {
                _walkerToSnapshot = walker;
                if (takeIncrementalSnapshot)
                {
                    _snapshotMap = PooledDictionary<BoundNode, Snapshot>.GetInstance();
                    _ownsMap = true;
                }
            }

            private Snapshotter(NullableWalker newWalker, PooledDictionary<BoundNode, Snapshot> snapshotMap)
            {
                _walkerToSnapshot = newWalker;
                _snapshotMap = snapshotMap;
                _ownsMap = false;
            }

            internal void Free()
            {
                if (_ownsMap)
                {
                    _snapshotMap?.Free();
                }
            }

            internal Snapshotter CreateForChildWalker(NullableWalker childWalker) =>
                new Snapshotter(childWalker, _snapshotMap);

            internal VariableState GetVariableState(Optional<LocalState> localState)
            {
                // We cache the last used dictionaries for _variableSlot and variableBySlot,
                // in order to minimize the number of dictionaries created. As we don't ever
                // remove or change things in these dictionaries, only add to them, we can just
                // track whether the underlying data structures have had their sizes changed since
                // the last time we captured them.
                // NullableWalker.variableBySlot is allocated in an exponential fashion: every time
                // a new slot is created that's off the end of the array, the array size is doubled.
                // Therefore, we only want to take the allocated identifiers, and verify that any slots
                // after the ones that we have are empty in Debug to verify. Slot 0 is always allocated,
                // so if _lastVariableBySlot is empty then we've never cached anything and need to cache
                if (_walkerToSnapshot._variableSlot.Count != _lastVariableSlot.Count
                    || _lastVariableBySlot.Length == 0)
                {
                    var variableBySlotBuilder = ArrayBuilder<VariableIdentifier>.GetInstance(_walkerToSnapshot.variableBySlot.Length);
                    var walkerVariableBySlot = _walkerToSnapshot.variableBySlot;
                    int i;
                    for (i = 0; i < walkerVariableBySlot.Length; i++)
                    {
                        // The first slot always does not exist for the purposes of caching and can be ignored
                        // in the exit condition check
                        if (i != 0 && !walkerVariableBySlot[i].Exists)
                        {
                            break;
                        }

                        variableBySlotBuilder.Add(walkerVariableBySlot[i]);
                    }

#if DEBUG
                    verifyRemainderNonExistant(walkerVariableBySlot, i);
#endif

                    _lastVariableBySlot = variableBySlotBuilder.ToImmutableAndFree();
                    _lastVariableSlot = _walkerToSnapshot._variableSlot.ToImmutableDictionary();
                }
#if DEBUG
                else
                {
                    // In debug mode, we verify the contents of the arrays to ensure that the invariant
                    // is maintained. Because variableBySlot is allocated exponentially, we can only
                    // verify that the length of our cached version is equal or less than the source
                    // array, and then verify that anything we haven't cached does not exist.
                    Debug.Assert(_lastVariableBySlot.Length <= _walkerToSnapshot.variableBySlot.Length);
                    int i;
                    for (i = 0; i < _lastVariableBySlot.Length; i++)
                    {
                        Debug.Assert(compareIdentifiers(_lastVariableBySlot[i], _walkerToSnapshot.variableBySlot[i]));
                    }
                    verifyRemainderNonExistant(_walkerToSnapshot.variableBySlot, i);

                    Debug.Assert(_lastVariableSlot.Count == _walkerToSnapshot._variableSlot.Count);
                    foreach ((VariableIdentifier key, int value) in _lastVariableSlot)
                    {
                        if (!key.Exists) continue;
                        Debug.Assert(_walkerToSnapshot._variableSlot.ContainsKey(key));
                        Debug.Assert(value == _walkerToSnapshot._variableSlot[key]);
                    }
                }
#endif

                return new VariableState(
                    _lastVariableSlot,
                    _lastVariableBySlot,
                    _walkerToSnapshot._variableTypes,
                    localState.HasValue ? localState.Value : _walkerToSnapshot.State.Clone());

                static bool compareIdentifiers(VariableIdentifier id1, VariableIdentifier id2) =>
                    (!id1.Exists && !id2.Exists) || id1.Equals(id2);

#if DEBUG
                static void verifyRemainderNonExistant(VariableIdentifier[] variablesBySlot, int startIndex)
                {
                    for (int i = startIndex; i < variablesBySlot.Length; i++)
                    {
                        Debug.Assert(!variablesBySlot[i].Exists);
                    }
                }
#endif
            }

            internal void SnapshotWalker(BoundNode node)
            {
                if (_snapshotMap != null)
                {
                    _snapshotMap[node] = Snapshot.SnapshotWalker(_walkerToSnapshot);
                }
            }

            internal ImmutableDictionary<BoundNode, Snapshot> Snapshots => _snapshotMap.ToImmutableDictionary();

            internal bool IncrementalSnapshotter => _snapshotMap != null;
        }

        /// <summary>
        /// Used to copy variable slots and types from the NullableWalker for the containing method
        /// or lambda to the NullableWalker created for a nested lambda or local function.
        /// </summary>
        internal sealed class VariableState
        {
            // Consider referencing the collections directly from the original NullableWalker
            // rather than copying the collections. (Items are added to the collections
            // but never replaced so the collections are lazily populated but otherwise immutable.)
            internal readonly ImmutableDictionary<VariableIdentifier, int> VariableSlot;
            internal readonly ImmutableArray<VariableIdentifier> VariableBySlot;
            internal readonly ImmutableDictionary<Symbol, TypeWithAnnotations> VariableTypes;

            // The nullable state of all variables captured at the point where the function or lambda appeared.
            internal readonly LocalState VariableNullableStates;

            internal VariableState(
                ImmutableDictionary<VariableIdentifier, int> variableSlot,
                ImmutableArray<VariableIdentifier> variableBySlot,
                ImmutableDictionary<Symbol, TypeWithAnnotations> variableTypes,
                LocalState variableNullableStates)
            {
                VariableSlot = variableSlot;
                VariableBySlot = variableBySlot;
                VariableTypes = variableTypes;
                VariableNullableStates = variableNullableStates;
            }
        }

        /// <summary>
        /// Contains a checkpoint for the NullableWalker at any given point of execution, used for restoring the walker to
        /// a specific point for speculatively analyzing a piece of code that does not appear in the original tree.
        /// </summary>
        internal readonly struct Snapshot
        {
            private readonly VariableState _variableState;
            private readonly bool _useMethodSignatureParameterTypes;
            private readonly Symbol _symbol;
            private readonly ImmutableDictionary<BoundExpression, TypeWithState> _methodGroupReceiverMapOpt;
            private readonly VisitResult _visitResult;
            private readonly VisitResult _currentConditionalReceiverVisitResult;
            private readonly ImmutableDictionary<object, PlaceholderLocal> _placeholderLocalsOpt;
            private readonly int _lastConditionalAccessSlot;

            private Snapshot(
                VariableState variableState,
                bool useMethodSignatureParameterTypes,
                Symbol symbol,
                ImmutableDictionary<BoundExpression, TypeWithState> methodGroupReceiverMapOpt,
                VisitResult visitResult,
                VisitResult currentConditionalReceiverVisitResult,
                ImmutableDictionary<object, PlaceholderLocal> placeholderLocalsOpt,
                int lastConditionalAccessSlot)
            {
                _variableState = variableState;
                _useMethodSignatureParameterTypes = useMethodSignatureParameterTypes;
                _symbol = symbol;
                _methodGroupReceiverMapOpt = methodGroupReceiverMapOpt;
                _visitResult = visitResult;
                _currentConditionalReceiverVisitResult = currentConditionalReceiverVisitResult;
                _placeholderLocalsOpt = placeholderLocalsOpt;
                _lastConditionalAccessSlot = lastConditionalAccessSlot;
            }

            internal static Snapshot SnapshotWalker(NullableWalker walker) =>
                new Snapshot(
                    walker._snapshotter.GetVariableState(walker.State),
                    walker._useMethodSignatureParameterTypes,
                    walker._symbol,
                    walker._methodGroupReceiverMapOpt,
                    walker._visitResult,
                    walker._currentConditionalReceiverVisitResult,
                    walker._placeholderLocalsOpt,
                    walker._lastConditionalAccessSlot);

            internal NullableWalker RestoreFromCheckpoint(CSharpCompilation compilation,
                                                     BoundNode nodeToAnalyze,
                                                     Binder binder,
                                                     Dictionary<BoundExpression, (NullabilityInfo, TypeSymbol)> analyzedNullabilityMap)
            {
                return new NullableWalker(
                    compilation,
                    this._symbol,
                    this._useMethodSignatureParameterTypes,
                    this._symbol as MethodSymbol,
                    nodeToAnalyze,
                    binder,
                    binder.Conversions,
                    this._variableState,
                    returnTypesOpt: default,
                    analyzedNullabilityMap,
                    takeIncrementalSnapshots: false)
                {
                    _methodGroupReceiverMapOpt = this._methodGroupReceiverMapOpt,
                    _visitResult = this._visitResult,
                    _currentConditionalReceiverVisitResult = this._currentConditionalReceiverVisitResult,
                    _placeholderLocalsOpt = this._placeholderLocalsOpt,
                    _lastConditionalAccessSlot = this._lastConditionalAccessSlot
                };
            }
        }
    }
}
