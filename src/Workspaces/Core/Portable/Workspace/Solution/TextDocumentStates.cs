// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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

// On NetFx, frozen dictionary is very expensive when you give it a case insensitive comparer.  This is due to
// unavoidable allocations it performs while doing its key-analysis that involve going through the non-span-aware
// culture types.  So, on netfx, we use a plain ReadOnlyDictionary here.
#if NET
using FilePathToDocumentIds = FrozenDictionary<string, OneOrMany<DocumentId>>;
#else
using FilePathToDocumentIds = ReadOnlyDictionary<string, OneOrMany<DocumentId>>;
#endif

/// <summary>
/// Holds on a <see cref="DocumentId"/> to <see cref="TextDocumentState"/> map and an ordering.
/// </summary>
internal sealed class TextDocumentStates<TState>
    where TState : TextDocumentState
{
#if NET
    private static readonly ObjectPool<Dictionary<string, OneOrMany<DocumentId>>> s_filePathPool = new(() => new(SolutionState.FilePathComparer));
#endif

    public static readonly TextDocumentStates<TState> Empty =
        new([],
            ImmutableSortedDictionary.Create<DocumentId, TState>(DocumentIdComparer.Instance),
#if NET
            FilePathToDocumentIds.Empty);
#else
            new(new Dictionary<string, OneOrMany<DocumentId>>()));
#endif

    private readonly ImmutableList<DocumentId> _ids;

    /// <summary>
    /// The entries in the map are sorted by <see cref="DocumentId.Id"/>, which yields locally deterministic order but not the order that
    /// matches the order in which documents were added. Therefore this ordering can't be used when creating compilations and it can't be 
    /// used when persisting document lists that do not preserve the GUIDs.
    /// </summary>
    private readonly ImmutableSortedDictionary<DocumentId, TState> _map;

    private FilePathToDocumentIds? _filePathToDocumentIds;

    private TextDocumentStates(
        ImmutableList<DocumentId> ids,
        ImmutableSortedDictionary<DocumentId, TState> map,
        FilePathToDocumentIds? filePathToDocumentIds)
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
        => SelectAsArray(
            selector: static (state, selector) => selector(state),
            arg: selector);

    public ImmutableArray<TValue> SelectAsArray<TValue, TArg>(TArg arg, Func<TState, TArg, TValue> selector)
    {
        var result = new FixedSizeArrayBuilder<TValue>(_map.Count);
        foreach (var (_, state) in _map)
            result.Add(selector(state, arg));

        return result.MoveToImmutable();
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

            if (ids.All(predicate: static (id, set) => set.Contains(id), arg: set))
                return Empty;
        }

        IEnumerable<DocumentId> enumerableIds = ids;
        return new(_ids.RemoveRange(enumerableIds), _map.RemoveRange(enumerableIds), filePathToDocumentIds: null);
    }

    internal TextDocumentStates<TState> SetState(TState state)
        => SetStates([state]);

    internal TextDocumentStates<TState> SetStates(ImmutableArray<TState> states)
    {
        var builder = _map.ToBuilder();
        var filePathToDocumentIds = _filePathToDocumentIds;

        foreach (var state in states)
        {
            var id = state.Id;
            var oldState = _map[id];

            // If any file paths have changed, don't preseve the computed map.  We'll regenerate the new map on demand when needed.
            if (filePathToDocumentIds != null && oldState.FilePath != state.FilePath)
                filePathToDocumentIds = null;

            builder[id] = state;
        }

        return new(_ids, builder.ToImmutable(), filePathToDocumentIds);
    }

    public TextDocumentStates<TState> UpdateStates<TArg>(TArg arg, Func<TState, TArg, TState> transformation)
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

    public async ValueTask<DocumentChecksumsAndIds> GetDocumentChecksumsAndIdsAsync(CancellationToken cancellationToken)
    {
        var attributeChecksums = new FixedSizeArrayBuilder<Checksum>(_map.Count);
        var textChecksums = new FixedSizeArrayBuilder<Checksum>(_map.Count);
        var documentIds = new FixedSizeArrayBuilder<DocumentId>(_map.Count);

        foreach (var (documentId, state) in _map)
        {
            var stateChecksums = await state.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            attributeChecksums.Add(stateChecksums.Info);
            textChecksums.Add(stateChecksums.Text);
            documentIds.Add(documentId);
        }

        return new(
            new ChecksumCollection(attributeChecksums.MoveToImmutable()),
            new ChecksumCollection(textChecksums.MoveToImmutable()),
            documentIds.MoveToImmutable());
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

    private FilePathToDocumentIds ComputeFilePathToDocumentIds()
    {
#if NET
        using var pooledDictionary = s_filePathPool.GetPooledObject();
        var result = pooledDictionary.Object;
#else
        var result = new Dictionary<string, OneOrMany<DocumentId>>(SolutionState.FilePathComparer);
#endif

        foreach (var (documentId, state) in _map)
        {
            var filePath = state.FilePath;
            if (filePath is null)
                continue;

            result[filePath] = result.TryGetValue(filePath, out var existingValue)
                ? existingValue.Add(documentId)
                : OneOrMany.Create(documentId);
        }

#if NET
        return result.ToFrozenDictionary(SolutionState.FilePathComparer);
#else
        return new(result);
#endif
    }
}
