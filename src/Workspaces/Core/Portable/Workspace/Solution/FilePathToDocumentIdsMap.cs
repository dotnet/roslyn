// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    public static readonly FilePathToDocumentIdsMap Empty = new(isFrozen: false, s_emptyMap);

    /// <summary>
    /// Whether or not this map corresponds to a frozen solution.  Frozen solutions commonly drop many documents
    /// (because only documents whose trees have been parsed are kept out).  To keep things fast, instead of actually
    /// dropping all those files from our <see cref="_map"/> we instead only keep track of added documents and mark that
    /// we're frozen.  Then, when a client actually asks for the document ids in a particular solution, we only return the
    /// actual set present in that solution instance.
    /// </summary>
    public readonly bool IsFrozen;
    private readonly ImmutableDictionary<string, ImmutableArray<DocumentId>> _map;

    private FilePathToDocumentIdsMap(bool isFrozen, ImmutableDictionary<string, ImmutableArray<DocumentId>> map)
    {
        IsFrozen = isFrozen;
        _map = map;
    }

    public bool TryGetValue(string filePath, out ImmutableArray<DocumentId> documentIdsWithPath)
        => _map.TryGetValue(filePath, out documentIdsWithPath);

    public static bool operator ==(FilePathToDocumentIdsMap left, FilePathToDocumentIdsMap right)
        => left.IsFrozen == right.IsFrozen && left._map == right._map;

    public static bool operator !=(FilePathToDocumentIdsMap left, FilePathToDocumentIdsMap right)
        => !(left == right);

    public override int GetHashCode()
        => throw new NotSupportedException();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is FilePathToDocumentIdsMap map && Equals(map);

    public Builder ToBuilder()
        => new(IsFrozen, _map.ToBuilder());

    public FilePathToDocumentIdsMap ToFrozen()
        => IsFrozen ? this : new(isFrozen: true, _map);

    public readonly struct Builder
    {
        private readonly bool _isFrozen;
        private readonly ImmutableDictionary<string, ImmutableArray<DocumentId>>.Builder _builder;

        public Builder(bool isFrozen, ImmutableDictionary<string, ImmutableArray<DocumentId>>.Builder builder)
        {
            _isFrozen = isFrozen;
            _builder = builder;
        }

        public FilePathToDocumentIdsMap ToImmutable()
            => new(_isFrozen, _builder.ToImmutable());

        public bool TryGetValue(string filePath, out ImmutableArray<DocumentId> documentIdsWithPath)
            => _builder.TryGetValue(filePath, out documentIdsWithPath);

        public void Add(string? filePath, DocumentId documentId)
        {
            if (RoslynString.IsNullOrEmpty(filePath))
                return;

            _builder.MultiAdd(filePath, documentId);
        }

        public void Remove(string? filePath, DocumentId documentId)
        {
            if (RoslynString.IsNullOrEmpty(filePath))
                return;

            if (!this.TryGetValue(filePath, out var documentIdsWithPath) || !documentIdsWithPath.Contains(documentId))
                throw new ArgumentException($"The given documentId was not found");

            _builder.MultiRemove(filePath, documentId);
        }
    }
}
