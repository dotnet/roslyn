// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Holds on a <see cref="DocumentId"/> to <see cref="TextDocumentState"/> map and an ordering.
    /// </summary>
    internal readonly struct TextDocumentStates<TState>
        where TState : TextDocumentState
    {
        public static readonly TextDocumentStates<TState> Empty =
            new(ImmutableList<DocumentId>.Empty, ImmutableSortedDictionary.Create<DocumentId, TState>(DocumentIdComparer.Instance));

        private readonly ImmutableList<DocumentId> _ids;

        /// <summary>
        /// The entries in the map are sorted by <see cref="DocumentId.Id"/>, which yields locally deterministic order but not the order that
        /// matches the order in which documents were added. Therefore this ordering can't be used when creating compilations and it can't be 
        /// used when persisting document lists that do not preserve the GUIDs.
        /// </summary>
        private readonly ImmutableSortedDictionary<DocumentId, TState> _map;

        private TextDocumentStates(ImmutableList<DocumentId> ids, ImmutableSortedDictionary<DocumentId, TState> map)
        {
            Debug.Assert(map.KeyComparer == DocumentIdComparer.Instance);

            _ids = ids;
            _map = map;
        }

        public TextDocumentStates(IEnumerable<TState> states)
            : this(states.Select(s => s.Id).ToImmutableList(),
                   states.ToImmutableSortedDictionary(state => state.Id, state => state, DocumentIdComparer.Instance))
        {
        }

        public TextDocumentStates(IEnumerable<DocumentInfo> infos, Func<DocumentInfo, TState> stateConstructor)
            : this(infos.Select(info => info.Id).ToImmutableList(),
                   infos.ToImmutableSortedDictionary(info => info.Id, stateConstructor, DocumentIdComparer.Instance))
        {
        }

        public TextDocumentStates<TState> WithCompilationOrder(ImmutableList<DocumentId> ids)
            => new(ids, _map);

        public int Count
            => _map.Count;

        public bool IsEmpty
            => Count == 0;

        public bool Contains(DocumentId id)
            => _map.ContainsKey(id);

        public bool TryGetState(DocumentId documentId, [NotNullWhen(true)] out TState? state)
            => _map.TryGetValue(documentId, out state);

        public TState? GetState(DocumentId documentId)
            => _map.TryGetValue(documentId, out var state) ? state : null;

        public TState GetRequiredState(DocumentId documentId)
            => _map.TryGetValue(documentId, out var state) ? state : throw ExceptionUtilities.Unreachable();

        /// <summary>
        /// <see cref="DocumentId"/>s in the order in which they were added to the project (the compilation order).
        /// </summary>
        public readonly IReadOnlyList<DocumentId> Ids => _ids;

        /// <summary>
        /// States ordered by <see cref="DocumentId"/>.
        /// </summary>
        public ImmutableSortedDictionary<DocumentId, TState> States
            => _map;

        /// <summary>
        /// Get states ordered in compilation order.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TState> GetStatesInCompilationOrder()
        {
            foreach (var id in Ids)
            {
                yield return _map[id];
            }
        }

        public ImmutableArray<TValue> SelectAsArray<TValue>(Func<TState, TValue> selector)
        {
            using var _ = ArrayBuilder<TValue>.GetInstance(out var builder);

            foreach (var (_, state) in _map)
            {
                builder.Add(selector(state));
            }

            return builder.ToImmutable();
        }

        public ImmutableArray<TValue> SelectAsArray<TValue, TArg>(Func<TState, TArg, TValue> selector, TArg arg)
        {
            using var _ = ArrayBuilder<TValue>.GetInstance(out var builder);

            foreach (var (_, state) in _map)
            {
                builder.Add(selector(state, arg));
            }

            return builder.ToImmutable();
        }

        public async ValueTask<ImmutableArray<TValue>> SelectAsArrayAsync<TValue, TArg>(Func<TState, TArg, CancellationToken, ValueTask<TValue>> selector, TArg arg, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<TValue>.GetInstance(out var builder);

            foreach (var (_, state) in _map)
            {
                builder.Add(await selector(state, arg, cancellationToken).ConfigureAwait(true));
            }

            return builder.ToImmutable();
        }

        public TextDocumentStates<TState> AddRange(ImmutableArray<TState> states)
            => new(_ids.AddRange(states.Select(state => state.Id)),
                   _map.AddRange(states.Select(state => KeyValuePairUtil.Create(state.Id, state))));

        public TextDocumentStates<TState> RemoveRange(ImmutableArray<DocumentId> ids)
        {
            IEnumerable<DocumentId> enumerableIds = ids;
            return new(_ids.RemoveRange(enumerableIds), _map.RemoveRange(enumerableIds));
        }

        internal TextDocumentStates<TState> SetState(DocumentId id, TState state)
            => new(_ids, _map.SetItem(id, state));

        public TextDocumentStates<TState> UpdateStates<TArg>(Func<TState, TArg, TState> transformation, TArg arg)
        {
            var builder = _map.ToBuilder();

            foreach (var (id, state) in _map)
            {
                builder[id] = transformation(state, arg);
            }

            return new(_ids, builder.ToImmutable());
        }

        /// <summary>
        /// Returns a <see cref="DocumentId"/>s of documents whose state changed when compared to older states.
        /// </summary>
        public IEnumerable<DocumentId> GetChangedStateIds(TextDocumentStates<TState> oldStates, bool ignoreUnchangedContent = false, bool ignoreUnchangeableDocuments = false)
        {
            Contract.ThrowIfTrue(!ignoreUnchangedContent && ignoreUnchangeableDocuments);

            foreach (var id in Ids)
            {
                if (!oldStates.TryGetState(id, out var oldState))
                {
                    // document was added
                    continue;
                }

                var newState = _map[id];
                if (newState == oldState)
                {
                    continue;
                }

                if (ignoreUnchangedContent && !newState.HasTextChanged(oldState, ignoreUnchangeableDocuments))
                {
                    continue;
                }

                yield return id;
            }
        }

        /// <summary>
        /// Returns a <see cref="DocumentId"/>s of added documents.
        /// </summary>
        public IEnumerable<DocumentId> GetAddedStateIds(TextDocumentStates<TState> oldStates)
            => (_ids == oldStates._ids) ? SpecializedCollections.EmptyEnumerable<DocumentId>() : Except(_ids, oldStates._map);

        /// <summary>
        /// Returns a <see cref="DocumentId"/>s of removed documents.
        /// </summary>
        public IEnumerable<DocumentId> GetRemovedStateIds(TextDocumentStates<TState> oldStates)
            => (_ids == oldStates._ids) ? SpecializedCollections.EmptyEnumerable<DocumentId>() : Except(oldStates._ids, _map);

        private static IEnumerable<DocumentId> Except(IEnumerable<DocumentId> ids, ImmutableSortedDictionary<DocumentId, TState> map)
        {
            foreach (var id in ids)
            {
                if (!map.ContainsKey(id))
                {
                    yield return id;
                }
            }
        }

        public bool HasAnyStateChanges(TextDocumentStates<TState> oldStates)
            => !_map.Values.SequenceEqual(oldStates._map.Values);

        private sealed class DocumentIdComparer : IComparer<DocumentId?>
        {
            public static readonly IComparer<DocumentId?> Instance = new DocumentIdComparer();

            private DocumentIdComparer()
            {
            }

            public int Compare(DocumentId? x, DocumentId? y)
            {
                if (x is null)
                {
                    return y is null ? 0 : -1;
                }
                else if (y is null)
                {
                    return 1;
                }

                return x.Id.CompareTo(y.Id);
            }
        }
    }
}
