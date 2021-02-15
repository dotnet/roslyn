// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
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
        private sealed class DocumentIdReadOnlyList : IReadOnlyList<DocumentId>
        {
            private readonly ImmutableSegmentedDictionary<DocumentId, TState> _dictionary;

            public DocumentIdReadOnlyList(ImmutableSegmentedDictionary<DocumentId, TState> dictionary)
                => _dictionary = dictionary;

            public DocumentId this[int index]
                => _dictionary.GetAddedEntry(index).Key;

            public int Count
                => _dictionary.Count;

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            public IEnumerator<DocumentId> GetEnumerator()
                => _dictionary.Keys.GetEnumerator();
        }

        public static readonly TextDocumentStates<TState> Empty = new(ImmutableSegmentedDictionary<DocumentId, TState>.Empty);

        public readonly IReadOnlyList<DocumentId> Ids;

        /// <summary>
        /// The entires in the map are enumerable and indexable (via <see cref="ImmutableSegmentedDictionary{DocumentId, TState}.GetAddedEntry(int)"/>) 
        /// in the order in which they have been added. When an item is removed we rehash the remaining entries so that we can keep enumerating and indexing
        /// in that order. This approach trades slower write operations for faster read operations.
        /// </summary>
        private readonly ImmutableSegmentedDictionary<DocumentId, TState> _map;

        private TextDocumentStates(ImmutableSegmentedDictionary<DocumentId, TState> map)
        {
            Ids = new DocumentIdReadOnlyList(map);
            _map = map;
        }

        public TextDocumentStates(IEnumerable<TState> states)
            : this(states.ToImmutableSegmentedDictionary(state => state.Id))
        {
        }

        public TextDocumentStates(IEnumerable<DocumentInfo> infos, Func<DocumentInfo, TState> stateConstructor)
            : this(infos.ToImmutableSegmentedDictionary(info => info.Id, stateConstructor))
        {
        }

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
            => _map.TryGetValue(documentId, out var state) ? state : throw ExceptionUtilities.Unreachable;

        /// <summary>
        /// States in the order they were added.
        /// </summary>
        public IEnumerable<TState> States
            => _map.Values;

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
            => new(_map.AddRange(states.Select(state => KeyValuePairUtil.Create(state.Id, state))));

        public TextDocumentStates<TState> RemoveRange(ImmutableArray<DocumentId> ids)
        {
            var builder = _map.ToBuilder();

            foreach (var id in ids)
            {
                builder.Remove(id);
            }

            // Have to call Compact in order to preserve ordering invariant.
            builder.Compact();

            return new(builder.ToImmutable());
        }

        internal TextDocumentStates<TState> SetState(DocumentId id, TState state)
            => new(_map.SetItem(id, state));

        public TextDocumentStates<TState> UpdateStates<TArg>(Func<TState, TArg, TState> transformation, TArg arg)
        {
            var builder = _map.ToBuilder();

            foreach (var (id, state) in _map)
            {
                builder[id] = transformation(state, arg);
            }

            return new(builder.ToImmutable());
        }

        /// <summary>
        /// Returns a <see cref="DocumentId"/>s of documents whose state changed when compared to older states.
        /// </summary>
        public IEnumerable<DocumentId> GetChangedStateIds(TextDocumentStates<TState> oldStates)
        {
            foreach (var (id, _) in _map)
            {
                if (oldStates._map.TryGetValue(id, out var oldState) && oldState != _map[id])
                {
                    yield return id;
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="DocumentId"/>s of added documents.
        /// </summary>
        public IEnumerable<DocumentId> GetAddedStateIds(TextDocumentStates<TState> oldStates)
        {
            foreach (var (id, _) in _map)
            {
                if (!oldStates._map.ContainsKey(id))
                {
                    yield return id;
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="DocumentId"/>s of removed documents.
        /// </summary>
        public IEnumerable<DocumentId> GetRemovedStateIds(TextDocumentStates<TState> oldStates)
        {
            foreach (var (id, _) in oldStates._map)
            {
                if (!_map.ContainsKey(id))
                {
                    yield return id;
                }
            }
        }

        public bool HasAnyStateChanges(TextDocumentStates<TState> oldStates)
            => !_map.Values.SequenceEqual(oldStates._map.Values);
    }
}
