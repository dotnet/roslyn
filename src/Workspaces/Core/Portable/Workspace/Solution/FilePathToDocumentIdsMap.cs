// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal readonly struct FilePathToDocumentIdsMap
    {
        private static readonly ImmutableDictionary<string, ImmutableArray<DocumentId>> s_emptyMap
            = ImmutableDictionary.Create<string, ImmutableArray<DocumentId>>(StringComparer.OrdinalIgnoreCase);
        public static readonly FilePathToDocumentIdsMap Empty = new(s_emptyMap);

        private readonly ImmutableDictionary<string, ImmutableArray<DocumentId>> _map;

        private FilePathToDocumentIdsMap(ImmutableDictionary<string, ImmutableArray<DocumentId>> map)
        {
            _map = map;
        }

        public bool TryGetValue(string filePath, out ImmutableArray<DocumentId> documentIdsWithPath)
                => _map.TryGetValue(filePath, out documentIdsWithPath);

        public static bool operator ==(FilePathToDocumentIdsMap left, FilePathToDocumentIdsMap right)
            => left._map == right._map;

        public static bool operator !=(FilePathToDocumentIdsMap left, FilePathToDocumentIdsMap right)
            => !(left == right);

        public override int GetHashCode()
            => throw new NotSupportedException();

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is FilePathToDocumentIdsMap map && Equals(map);

        public Builder ToBuilder()
            => new(_map.ToBuilder());

        public readonly struct Builder
        {
            private readonly ImmutableDictionary<string, ImmutableArray<DocumentId>>.Builder _builder;

            public Builder(ImmutableDictionary<string, ImmutableArray<DocumentId>>.Builder builder)
            {
                _builder = builder;
            }

            public FilePathToDocumentIdsMap ToImmutable()
                => new(_builder.ToImmutable());

            public bool TryGetValue(string filePath, out ImmutableArray<DocumentId> documentIdsWithPath)
                => _builder.TryGetValue(filePath, out documentIdsWithPath);

            public void MultiAdd(string filePath, DocumentId documentId)
                => _builder.MultiAdd(filePath, documentId);

            public void MultiRemove(string filePath, DocumentId documentId)
                => _builder.MultiRemove(filePath, documentId);
        }
    }
}
