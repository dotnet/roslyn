// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Holds on a <see cref="DocumentId"/> to <see cref="TextDocumentState"/> map and an ordering.
    /// </summary>
    internal readonly struct TextDocumentStates<TState>
        where TState : TextDocumentState
    {
        public static readonly TextDocumentStates<TState> Empty = new(ImmutableArray<DocumentId>.Empty, ImmutableDictionary<DocumentId, TState>.Empty);

        /// <summary>
        /// Ordered <see cref="DocumentId"/>s. This is always an <see cref="ImmutableArray{T}"/>,
        /// but we hold on its boxed instance to avoid re-boxing when returning this value from public APIs.
        /// </summary>
        public readonly IReadOnlyList<DocumentId> Ids;

        public readonly ImmutableDictionary<DocumentId, TState> Map;

        private TextDocumentStates(IReadOnlyList<DocumentId> ids, ImmutableDictionary<DocumentId, TState> map)
        {
            Debug.Assert(ids.All(id => map.ContainsKey(id)) && ids.Count == map.Count);

            Ids = ids;
            Map = map;
        }

        public TextDocumentStates(ImmutableArray<DocumentId> ids, ImmutableDictionary<DocumentId, TState> map)
            : this((IReadOnlyList<DocumentId>)ids, map)
        {
        }

        public TextDocumentStates(IReadOnlyList<TState> states)
            : this(states.SelectAsArray(state => state.Id),
                   states.ToImmutableDictionary(state => state.Id))
        {
        }

        public TextDocumentStates(IEnumerable<DocumentInfo> infos, Func<DocumentInfo, TState> stateConstructor)
            : this(infos.Select(info => info.Id).ToImmutableArray(),
                   infos.ToImmutableDictionary(info => info.Id, stateConstructor))
        {
        }

        public int Count
            => Ids.Count;

        public bool IsEmpty
            => Count == 0;

        public bool Contains(DocumentId id)
            => Map.ContainsKey(id);

        public TState? GetValue(DocumentId documentId)
            => Map.TryGetValue(documentId, out var state) ? state : null;

        public TState GetRequiredValue(DocumentId documentId)
            => Map.TryGetValue(documentId, out var state) ? state : throw ExceptionUtilities.Unreachable;

        public IEnumerable<TState> Values
        {
            get
            {
                foreach (var id in Ids)
                {
                    yield return Map[id];
                }
            }
        }

        private ImmutableArray<DocumentId> IdArray
            => (ImmutableArray<DocumentId>)Ids;

        public ImmutableArray<TValue> SelectAsArray<TValue>(Func<TState, TValue> selector)
            => IdArray.SelectAsArray(static (id, args) => args.selector(args.Map[id]), (Map, selector));

        public ImmutableArray<TValue> SelectAsArray<TValue, TArg>(Func<TState, TArg, TValue> selector, TArg arg)
            => IdArray.SelectAsArray(static (id, args) => args.selector(args.Map[id], args.arg), (Map, selector, arg));

        public TextDocumentStates<TState> AddRange(ImmutableArray<TState> states)
            => new(IdArray.AddRange(states.Select(state => state.Id)),
                    Map.AddRange(states.Select(state => KeyValuePairUtil.Create(state.Id, state))));

        public TextDocumentStates<TState> RemoveRange(ImmutableArray<DocumentId> ids)
            => new(IdArray.RemoveRange(ids), Map.RemoveRange(ids));

        internal TextDocumentStates<TState> SetValue(DocumentId id, TState state)
            => new(Ids, Map.SetItem(id, state));

        public TextDocumentStates<TState> UpdateValues<TArg>(Func<TState, TArg, TState> transformation, TArg arg)
        {
            var newMap = Map;
            foreach (var (id, state) in Map)
            {
                newMap = newMap.SetItem(id, transformation(state, arg));
            }

            return new(Ids, newMap);
        }

        /// <summary>
        /// Returns a <see cref="DocumentId"/>s of documents whose state changed when compared to older states.
        /// </summary>
        public IEnumerable<DocumentId> GetChangedStateIds(TextDocumentStates<TState> oldStates)
        {
            foreach (var id in Ids)
            {
                if (oldStates.Map.TryGetValue(id, out var oldState) && oldState != Map[id])
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
            foreach (var id in Ids)
            {
                if (!oldStates.Map.ContainsKey(id))
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
            foreach (var id in oldStates.Ids)
            {
                if (!Map.ContainsKey(id))
                {
                    yield return id;
                }
            }
        }

        public bool HasAnyStateChanges(TextDocumentStates<TState> oldStates)
            => !Values.SequenceEqual(oldStates.Values);
    }
}
