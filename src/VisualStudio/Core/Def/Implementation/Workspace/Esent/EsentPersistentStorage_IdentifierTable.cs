// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Versions;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentPersistentStorage : ISyntaxTreeInfoPersistentStorage
    {
        private const string IdentifierSetVersion = "<IdentifierSetVersion>";
        private const string IdentifierSetSerializationVersion = "1";
        private const int NotSupported = -1;
        private const int FlushThreshold = 5000;

        private int? _identifierSetVersionId;

        private bool TryGetUniqueIdentifierId(string identifier, out int id)
        {
            id = default(int);

            if (string.IsNullOrWhiteSpace(identifier))
            {
                return false;
            }

            try
            {
                id = _esentStorage.GetUniqueIdentifierId(identifier);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryGetIdentifierSetVersionId(out int id)
        {
            if (_identifierSetVersionId.HasValue)
            {
                id = _identifierSetVersionId.Value;
                return true;
            }

            if (TryGetUniqueIdentifierId(IdentifierSetVersion, out id))
            {
                _identifierSetVersionId = id;
                return true;
            }

            return false;
        }

        private VersionStamp GetIdentifierSetVersion(EsentStorage.Key key)
        {
            if (!PersistenceEnabled)
            {
                return VersionStamp.Default;
            }

            int identifierId;
            if (!TryGetIdentifierSetVersionId(out identifierId))
            {
                return VersionStamp.Default;
            }

            // TODO: verify that project is in solution associated with the storage
            return EsentExceptionWrapper(key, identifierId, GetIdentifierSetVersion, CancellationToken.None);
        }

        private VersionStamp GetIdentifierSetVersion(EsentStorage.Key key, int identifierId, object unused1, object unused2, CancellationToken cancellationToken)
        {
            using (var accessor = _esentStorage.GetIdentifierLocationTableAccessor())
            using (var stream = accessor.GetReadStream(key, identifierId))
            {
                if (stream == null)
                {
                    return VersionStamp.Default;
                }

                using (var reader = new ObjectReader(stream))
                {
                    return VersionStamp.ReadFrom(reader);
                }
            }
        }

        public VersionStamp GetIdentifierSetVersion(Document document)
        {
            if (!PersistenceEnabled)
            {
                return VersionStamp.Default;
            }

            EsentStorage.Key key;
            if (!TryGetProjectAndDocumentKey(document, out key))
            {
                return VersionStamp.Default;
            }

            return GetIdentifierSetVersion(key);
        }

        public bool ReadIdentifierPositions(Document document, VersionStamp syntaxVersion, string identifier, List<int> positions, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(identifier));

            if (!PersistenceEnabled)
            {
                return false;
            }

            int identifierId;
            EsentStorage.Key key;
            if (!TryGetProjectAndDocumentKey(document, out key))
            {
                return false;
            }

            var persistedVersion = GetIdentifierSetVersion(key);
            if (!document.CanReusePersistedSyntaxTreeVersion(syntaxVersion, persistedVersion))
            {
                return false;
            }

            if (!TryGetUniqueIdentifierId(identifier, out identifierId))
            {
                return false;
            }

            return EsentExceptionWrapper(key, identifierId, positions, ReadIdentifierPositions, cancellationToken);
        }

        private bool ReadIdentifierPositions(EsentStorage.Key key, int identifierId, List<int> positions, object unused, CancellationToken cancellationToken)
        {
            using (var accessor = _esentStorage.GetIdentifierLocationTableAccessor())
            using (var stream = accessor.GetReadStream(key, identifierId))
            {
                if (stream == null)
                {
                    // no such identifier exist.
                    return true;
                }

                using (var reader = new ObjectReader(stream))
                {
                    var formatVersion = reader.ReadString();
                    if (formatVersion != IdentifierSetSerializationVersion)
                    {
                        return false;
                    }

                    return ReadFrom(reader, positions, cancellationToken);
                }
            }
        }

        private bool DeleteIdentifierLocations(EsentStorage.Key key, CancellationToken cancellationToken)
        {
            using (var accessor = _esentStorage.GetIdentifierLocationTableAccessor())
            {
                accessor.Delete(key, cancellationToken);
                return accessor.ApplyChanges();
            }
        }

        public bool WriteIdentifierLocations(Document document, VersionStamp version, SyntaxNode root, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(root);

            if (!PersistenceEnabled)
            {
                return false;
            }

            EsentStorage.Key key;
            if (!TryGetProjectAndDocumentKey(document, out key))
            {
                return false;
            }

            return EsentExceptionWrapper(key, document, version, root, WriteIdentifierLocations, cancellationToken);
        }

        private bool WriteIdentifierLocations(EsentStorage.Key key, Document document, VersionStamp version, SyntaxNode root, CancellationToken cancellationToken)
        {
            // delete any existing data
            if (!DeleteIdentifierLocations(key, cancellationToken))
            {
                return false;
            }

            var identifierMap = SharedPools.StringIgnoreCaseDictionary<int>().AllocateAndClear();

            Dictionary<string, List<int>> map = null;
            try
            {
                map = CreateIdentifierLocations(document, root, cancellationToken);

                // okay, write new data
                using (var accessor = _esentStorage.GetIdentifierLocationTableAccessor())
                {
                    // make sure I have all identifier ready before starting big insertion
                    int identifierId;
                    foreach (var identifier in map.Keys)
                    {
                        if (!TryGetUniqueIdentifierId(identifier, out identifierId))
                        {
                            return false;
                        }

                        identifierMap[identifier] = identifierId;
                    }

                    // save whole map
                    var uncommittedCount = 0;

                    foreach (var kv in map)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var identifier = kv.Key;
                        var positions = kv.Value;

                        if ((uncommittedCount + positions.Count) > FlushThreshold)
                        {
                            accessor.Flush();
                            uncommittedCount = 0;
                        }

                        accessor.PrepareBatchOneInsert();

                        identifierId = identifierMap[identifier];

                        using (var stream = accessor.GetWriteStream(key, identifierId))
                        using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
                        {
                            writer.WriteString(IdentifierSetSerializationVersion);
                            WriteList(writer, positions);
                        }

                        accessor.FinishBatchOneInsert();

                        uncommittedCount += positions.Count;
                    }

                    // save special identifier that indicates version for this document
                    if (!TrySaveIdentifierSetVersion(accessor, key, version))
                    {
                        return false;
                    }

                    return accessor.ApplyChanges();
                }
            }
            finally
            {
                SharedPools.StringIgnoreCaseDictionary<int>().ClearAndFree(identifierMap);
                Free(map);
            }
        }

        private void Free(Dictionary<string, List<int>> map)
        {
            if (map == null)
            {
                return;
            }

            foreach (var value in map.Values)
            {
                if (value == null)
                {
                    continue;
                }

                SharedPools.BigDefault<List<int>>().ClearAndFree(value);
            }

            SharedPools.StringIgnoreCaseDictionary<List<int>>().ClearAndFree(map);
        }

        private bool TrySaveIdentifierSetVersion(
            EsentStorage.IdentifierLocationTableAccessor accessor, EsentStorage.Key key, VersionStamp version)
        {
            int identifierId;
            if (!TryGetIdentifierSetVersionId(out identifierId))
            {
                return false;
            }

            accessor.PrepareBatchOneInsert();
            using (var stream = accessor.GetWriteStream(key, identifierId))
            using (var writer = new ObjectWriter(stream))
            {
                version.WriteTo(writer);
            }

            accessor.FinishBatchOneInsert();
            return true;
        }

        private bool ReadFrom(ObjectReader reader, List<int> result, CancellationToken cancellationToken)
        {
            var count = reader.ReadInt32();
            if (count < 0)
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.Add(reader.ReadInt32());
            }

            return true;
        }

        private void WriteList(ObjectWriter writer, List<int> positions)
        {
            if (positions.Count > FlushThreshold)
            {
                writer.WriteInt32(NotSupported);
                return;
            }

            writer.WriteInt32(positions.Count);

            foreach (var position in positions)
            {
                writer.WriteInt32(position);
            }
        }

        private static Dictionary<string, List<int>> CreateIdentifierLocations(Document document, SyntaxNode root, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var identifierMap = SharedPools.StringIgnoreCaseDictionary<List<int>>().AllocateAndClear();
            foreach (var token in root.DescendantTokens(descendIntoTrivia: true))
            {
                if (token.IsMissing || token.Span.Length == 0)
                {
                    continue;
                }

                if (syntaxFacts.IsIdentifier(token) || syntaxFacts.IsGlobalNamespaceKeyword(token))
                {
                    var valueText = token.ValueText;
                    identifierMap.GetOrAdd(valueText, _ => SharedPools.BigDefault<List<int>>().AllocateAndClear()).Add(token.Span.Start);
                }
            }

            return identifierMap;
        }
    }
}
