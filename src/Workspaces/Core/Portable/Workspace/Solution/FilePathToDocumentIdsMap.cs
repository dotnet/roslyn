// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Helper type that keeps track of all the file paths for all the documents in a solution snapshot and all the document
/// ids each maps to.
/// </summary>
internal readonly struct FilePathToDocumentIdsMap
{
    private static readonly ImmutableDictionary<string, ImmutableArray<DocumentId>> s_emptyMap
        = ImmutableDictionary.Create<string, ImmutableArray<DocumentId>>(StringComparer.OrdinalIgnoreCase);
    public static readonly FilePathToDocumentIdsMap Empty = new(isFrozen: false, lazyMap: new Lazy<ImmutableDictionary<string, ImmutableArray<DocumentId>>>(() => s_emptyMap));

    /// <summary>
    /// Whether or not this map corresponds to a frozen solution.  Frozen solutions commonly drop many documents
    /// (because only documents whose trees have been parsed are kept out).  To keep things fast, instead of actually
    /// dropping all those files from our <see cref="_lazyMap"/> we instead only keep track of added documents and mark that
    /// we're frozen.  Then, when a client actually asks for the document ids in a particular solution, we only return the
    /// actual set present in that solution instance.
    /// </summary>
    public readonly bool IsFrozen;
    private readonly Lazy<ImmutableDictionary<string, ImmutableArray<DocumentId>>> _lazyMap;

    private FilePathToDocumentIdsMap(bool isFrozen, Lazy<ImmutableDictionary<string, ImmutableArray<DocumentId>>> lazyMap)
    {
        IsFrozen = isFrozen;
        _lazyMap = lazyMap;
    }

    public bool TryGetValue(string filePath, out ImmutableArray<DocumentId> documentIdsWithPath)
    {
        return _lazyMap.Value.TryGetValue(filePath, out documentIdsWithPath);
    }

    public static bool operator ==(FilePathToDocumentIdsMap left, FilePathToDocumentIdsMap right)
    {
        return left.IsFrozen == right.IsFrozen && left._lazyMap.Value == right._lazyMap.Value;
    }

    public static bool operator !=(FilePathToDocumentIdsMap left, FilePathToDocumentIdsMap right)
        => !(left == right);

    public override int GetHashCode()
        => throw new NotSupportedException();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is FilePathToDocumentIdsMap map && Equals(map);

    public Builder ToBuilder()
    {
        return new(IsFrozen, _lazyMap);
    }

    public FilePathToDocumentIdsMap ToFrozen()
    {
        return IsFrozen ? this : new(isFrozen: true, _lazyMap);
    }

    public class Builder
    {
        private readonly bool _isFrozen;
        private readonly Lazy<ImmutableDictionary<string, ImmutableArray<DocumentId>>> _lazyMap;

        private readonly List<ChangeKind> _changeKinds = new();
        private List<(string FilePath, DocumentId DocumentId)>? _itemData;
        private List<IEnumerable<TextDocumentState>>? _rangeData;

        public Builder(bool isFrozen, Lazy<ImmutableDictionary<string, ImmutableArray<DocumentId>>> lazyMap)
        {
            _isFrozen = isFrozen;
            _lazyMap = lazyMap;
        }

        public FilePathToDocumentIdsMap ToImmutable()
        {
            return new(_isFrozen, new Lazy<ImmutableDictionary<string, ImmutableArray<DocumentId>>>(GetMap));
        }

        internal enum ChangeKind
        {
            Add,
            AddRange,
            Remove,
            RemoveRange
        }

        public void AddRange(IEnumerable<TextDocumentState> documentStates)
        {
            _changeKinds.Add(ChangeKind.AddRange);

            if (_rangeData is null)
                _rangeData = [];

            _rangeData.Add(documentStates);
        }

        public void Add(string? filePath, DocumentId documentId)
        {
            if (RoslynString.IsNullOrEmpty(filePath))
                return;

            _changeKinds.Add(ChangeKind.Add);

            if (_itemData is null)
                _itemData = [];

            _itemData.Add((filePath, documentId));
        }

        public void RemoveRange(IEnumerable<TextDocumentState> documentStates)
        {
            _changeKinds.Add(ChangeKind.RemoveRange);

            if (_rangeData is null)
                _rangeData = [];

            _rangeData.Add(documentStates);
        }

        public void Remove(string? filePath, DocumentId documentId)
        {
            if (RoslynString.IsNullOrEmpty(filePath))
                return;

            _changeKinds.Add(ChangeKind.Remove);

            if (_itemData is null)
                _itemData = [];

            _itemData.Add((filePath, documentId));
        }

        private ImmutableDictionary<string, ImmutableArray<DocumentId>> GetMap()
        {
            var itemIndex = 0;
            var rangeIndex = 0;
            var builder = _lazyMap.Value.ToBuilder();

            foreach (var changeKind in _changeKinds)
            {
                switch (changeKind)
                {
                    case ChangeKind.Add:
                        var addItemData = _itemData![itemIndex];
                        itemIndex++;
                        builder.MultiAdd(addItemData.FilePath, addItemData.DocumentId);
                        break;
                    case ChangeKind.AddRange:
                        var addRangeData = _rangeData![rangeIndex];
                        rangeIndex++;
                        foreach (var textDocumentData in addRangeData)
                        {
                            if (!RoslynString.IsNullOrEmpty(textDocumentData.FilePath))
                                builder.MultiAdd(textDocumentData.FilePath, textDocumentData.Id);
                        }
                        break;
                    case ChangeKind.Remove:
                        var removeItemData = _itemData![itemIndex];
                        itemIndex++;
                        builder.MultiRemove(removeItemData.FilePath, removeItemData.DocumentId);
                        break;
                    case ChangeKind.RemoveRange:
                        var removeRangeData = _rangeData![rangeIndex];
                        rangeIndex++;
                        foreach (var textDocumentData in removeRangeData)
                        {
                            if (!RoslynString.IsNullOrEmpty(textDocumentData.FilePath))
                                builder.MultiRemove(textDocumentData.FilePath, textDocumentData.Id);
                        }
                        break;
                }
            }

            return builder.ToImmutable();
        }
    }
}
