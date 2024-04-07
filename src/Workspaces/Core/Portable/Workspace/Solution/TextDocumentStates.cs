﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Holds on a <see cref="DocumentId"/> to <see cref="TextDocumentState"/> map and an ordering.
/// </summary>
internal sealed class TextDocumentStates<TState>
    where TState : TextDocumentState
{
    public static readonly TextDocumentStates<TState> Empty =
        new([], ImmutableSortedDictionary.Create<DocumentId, TState>(DocumentIdComparer.Instance), FrozenDictionary<string, OneOrMany<DocumentId>>.Empty);

    private readonly ImmutableList<DocumentId> _ids;

    /// <summary>
    /// The entries in the map are sorted by <see cref="DocumentId.Id"/>, which yields locally deterministic order but not the order that
    /// matches the order in which documents were added. Therefore this ordering can't be used when creating compilations and it can't be 
    /// used when persisting document lists that do not preserve the GUIDs.
    /// </summary>
    private readonly ImmutableSortedDictionary<DocumentId, TState> _map;

    private FrozenDictionary<string, OneOrMany<DocumentId>>? _filePathToDocumentIds;

    private TextDocumentStates(
        ImmutableList<DocumentId> ids,
        ImmutableSortedDictionary<DocumentId, TState> map,
        FrozenDictionary<string, OneOrMany<DocumentId>>? filePathToDocumentIds)
    {
        Debug.Assert(map.KeyComparer == DocumentIdComparer.Instance);

        _ids = ids;
        _map = map;
        _filePathToDocumentIds = filePathToDocumentIds;
    }

    public TextDocumentStates(IEnumerable<TState> states)
        : this(states.Select(s => s.Id).ToImmutableList(),
               states.ToImmutableSortedDictionary(state => state.Id, state => state, DocumentIdComparer.Instance),
               filePathToDocumentIds: null)
    {
    }

    public TextDocumentStates(IEnumerable<DocumentInfo> infos, Func<DocumentInfo, TState> stateConstructor)
        : this(infos.Select(info => info.Id).ToImmutableList(),
               infos.ToImmutableSortedDictionary(info => info.Id, stateConstructor, DocumentIdComparer.Instance),
               filePathToDocumentIds: null)
    {
    }

    public TextDocumentStates<TState> WithCompilationOrder(ImmutableList<DocumentId> ids)
        => new(ids, _map, _filePathToDocumentIds);

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
    public IReadOnlyList<DocumentId> Ids => _ids;

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
        var map = _map;
        return Ids.Select(id => map[id]);
    }

    public ImmutableArray<TValue> SelectAsArray<TValue>(Func<TState, TValue> selector)
    {
        // Directly use ImmutableArray.Builder as we know the final size
        var builder = ImmutableArray.CreateBuilder<TValue>(_map.Count);

        foreach (var (_, state) in _map)
        {
            builder.Add(selector(state));
        }

        return builder.MoveToImmutable();
    }

    public ImmutableArray<TValue> SelectAsArray<TValue, TArg>(Func<TState, TArg, TValue> selector, TArg arg)
    {
        // Directly use ImmutableArray.Builder as we know the final size
        var builder = ImmutableArray.CreateBuilder<TValue>(_map.Count);

        foreach (var (_, state) in _map)
        {
            builder.Add(selector(state, arg));
        }

        return builder.MoveToImmutable();
    }

    public async ValueTask<ImmutableArray<TValue>> SelectAsArrayAsync<TValue, TArg>(Func<TState, TArg, CancellationToken, ValueTask<TValue>> selector, TArg arg, CancellationToken cancellationToken)
    {
        // Directly use ImmutableArray.Builder as we know the final size
        var builder = ImmutableArray.CreateBuilder<TValue>(_map.Count);

        foreach (var (_, state) in _map)
        {
            builder.Add(await selector(state, arg, cancellationToken).ConfigureAwait(true));
        }

        return builder.MoveToImmutable();
    }

    public TextDocumentStates<TState> AddRange(ImmutableArray<TState> states)
        => new(_ids.AddRange(states.Select(state => state.Id)),
               _map.AddRange(states.Select(state => KeyValuePairUtil.Create(state.Id, state))),
               filePathToDocumentIds: null);

    public TextDocumentStates<TState> RemoveRange(ImmutableArray<DocumentId> ids)
    {
        if (ids.Length == _ids.Count)
        {
            using var _ = PooledHashSet<DocumentId>.GetInstance(out var set);

#if NETCOREAPP
            set.EnsureCapacity(ids.Length);
#endif

            foreach (var documentId in _ids)
                set.Add(documentId);

            if (ids.All(static (id, set) => set.Contains(id), set))
                return Empty;
        }

        IEnumerable<DocumentId> enumerableIds = ids;
        return new(_ids.RemoveRange(enumerableIds), _map.RemoveRange(enumerableIds), filePathToDocumentIds: null);
    }

    internal TextDocumentStates<TState> SetState(DocumentId id, TState state)
    {
        var oldState = _map[id];
        var filePathToDocumentIds = oldState.FilePath != state.FilePath
            ? null
            : _filePathToDocumentIds;

        return new(_ids, _map.SetItem(id, state), filePathToDocumentIds);
    }

    public TextDocumentStates<TState> UpdateStates<TArg>(Func<TState, TArg, TState> transformation, TArg arg)
    {
        var builder = _map.ToBuilder();
        var filePathsChanged = false;
        foreach (var (id, state) in _map)
        {
            var newState = transformation(state, arg);

            // Track if the file path changed when updating any of the state values.
            filePathsChanged = filePathsChanged || newState.FilePath != state.FilePath;

            builder[id] = newState;
        }

        // If any file paths changed, don't pass along our computed map.  We'll recompute it on demand when needed.
        var filePaths = filePathsChanged
            ? null
            : _filePathToDocumentIds;
        return new(_ids, builder.ToImmutable(), filePaths);
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
        => (_ids == oldStates._ids) ? [] : Except(_ids, oldStates._map);

    /// <summary>
    /// Returns a <see cref="DocumentId"/>s of removed documents.
    /// </summary>
    public IEnumerable<DocumentId> GetRemovedStateIds(TextDocumentStates<TState> oldStates)
        => (_ids == oldStates._ids) ? [] : Except(oldStates._ids, _map);

    private static IEnumerable<DocumentId> Except(ImmutableList<DocumentId> ids, ImmutableSortedDictionary<DocumentId, TState> map)
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

    public override bool Equals(object? obj)
        => obj is TextDocumentStates<TState> other && Equals(other);

    public override int GetHashCode()
        => throw new NotSupportedException();

    public bool Equals(TextDocumentStates<TState> other)
        => _map == other._map && _ids == other.Ids;

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

    public async ValueTask<ChecksumsAndIds<DocumentId>> GetChecksumsAndIdsAsync(CancellationToken cancellationToken)
    {
        var documentChecksumTasks = SelectAsArray(static (state, token) => state.GetChecksumAsync(token), cancellationToken);
        var documentChecksums = new ChecksumCollection(await documentChecksumTasks.WhenAll().ConfigureAwait(false));
        return new(documentChecksums, SelectAsArray(static s => s.Id));
    }

    public void AddDocumentIdsWithFilePath(ref TemporaryArray<DocumentId> temporaryArray, string filePath)
    {
        // Lazily initialize the file path map if not computed.
        _filePathToDocumentIds ??= ComputeFilePathToDocumentIds();

        if (_filePathToDocumentIds.TryGetValue(filePath, out var oneOrMany))
        {
            foreach (var value in oneOrMany)
                temporaryArray.Add(value);
        }
    }

    public DocumentId? GetFirstDocumentIdWithFilePath(string filePath)
    {
        // Lazily initialize the file path map if not computed.
        _filePathToDocumentIds ??= ComputeFilePathToDocumentIds();

        // Safe to call .First here as the values in the _filePathToDocumentIds dictionary will never empty.
        return _filePathToDocumentIds.TryGetValue(filePath, out var oneOrMany)
            ? oneOrMany.First()
            : null;
    }

    private FrozenDictionary<string, OneOrMany<DocumentId>> ComputeFilePathToDocumentIds()
    {
        using var _ = PooledDictionary<string, OneOrMany<DocumentId>>.GetInstance(out var result);

        foreach (var (documentId, state) in _map)
        {
            var filePath = state.FilePath;
            if (filePath is null)
                continue;

            result[filePath] = result.TryGetValue(filePath, out var existingValue)
                ? existingValue.Add(documentId)
                : OneOrMany.Create(documentId);
        }

        return result.ToFrozenDictionary();
    }
}
