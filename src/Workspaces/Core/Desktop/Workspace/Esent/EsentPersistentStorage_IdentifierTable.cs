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

namespace Microsoft.CodeAnalysis.Esent
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

            if (!TryGetIdentifierSetVersionId(out var identifierId))
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

        public bool ReadIdentifierPositions(Document document, VersionStamp syntaxVersion, string identifier, List<int> positions, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(identifier));

            if (!PersistenceEnabled)
            {
                return false;
            }

            if (!TryGetProjectAndDocumentKey(document, out var key))
            {
                return false;
            }

            var persistedVersion = GetIdentifierSetVersion(key);
            if (!document.CanReusePersistedSyntaxTreeVersion(syntaxVersion, persistedVersion))
            {
                return false;
            }

            if (!TryGetUniqueIdentifierId(identifier, out var identifierId))
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
