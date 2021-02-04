// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteDiagnosticCacheService : BrokeredServiceBase, IRemoteDiagnosticCacheService
    {
        private const string PersistenceName = "<CachedDiagnsoticsForDocument>";

        private const int MaxCachedDocumentCount = 10;
        private readonly LinkedList<(DocumentId id, Checksum checksum, ImmutableArray<DiagnosticData> diagnostics)> _cachedData = new();

        internal sealed class Factory : FactoryBase<IRemoteDiagnosticCacheService>
        {
            protected override IRemoteDiagnosticCacheService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteDiagnosticCacheService(arguments);
        }

        public RemoteDiagnosticCacheService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        public ValueTask CacheDiagnosticsAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, ImmutableArray<DiagnosticData> diagnostics, CancellationToken cancellationToken)
            => RunServiceAsync(async cancellationToken =>
            {
                lock (_cachedData)
                    _cachedData.Clear();

                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var document = solution.GetRequiredDocument(documentId);
                await CacheDiagnosticsAsync(document, diagnostics, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);

        public ValueTask<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(SerializableDocumentKey documentKey, Checksum checksum, CancellationToken cancellationToken)
            => RunServiceAsync(async cancellationToken =>
                await GetCachedDiagnosticsAsync(documentKey.Rehydrate(), checksum, cancellationToken).ConfigureAwait(false),
                cancellationToken);

        private static async Task CacheDiagnosticsAsync(Document document, ImmutableArray<DiagnosticData> diagnostics, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var workspace = solution.Workspace;

            if (workspace.Services.GetService<IPersistentStorageService>() is not IChecksummedPersistentStorageService persistenceService)
                return;

            using var storage = await persistenceService.GetStorageAsync(solution, cancellationToken).ConfigureAwait(false);
            if (storage == null)
                return;

            using var stream = SerializableBytes.CreateWritableStream();
            using (var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken))
            {
                DiagnosticDataSerializer.WriteDiagnosticDataNoProjectInfo(writer, diagnostics, cancellationToken);
            }

            stream.Position = 0;

            var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);
            await storage.WriteStreamAsync(DocumentKey.ToDocumentKey(document), PersistenceName, stream, checksum, cancellationToken).ConfigureAwait(false);

            static async ValueTask<Checksum> GetChecksumAsync(Document document, CancellationToken cancellationToken)
            {
                var checksums = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                return checksums.Text;
            }
        }

        private async Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(DocumentKey documentKey, Checksum checksum, CancellationToken cancellationToken)
        {
            if (TryGetFromInMemoryCache(documentKey, checksum, out var diagnostics))
                return diagnostics;

            var workspace = GetWorkspace();
            if (workspace.Services.GetService<IPersistentStorageService>() is not IChecksummedPersistentStorageService persistenceService)
                return ImmutableArray<DiagnosticData>.Empty;

            using var storage = await persistenceService.GetStorageAsync(workspace, documentKey.Project.Solution, checkBranchId: false, cancellationToken).ConfigureAwait(false);
            if (storage == null)
                return ImmutableArray<DiagnosticData>.Empty;

            using var stream = await storage.ReadStreamAsync(documentKey, PersistenceName, checksum, cancellationToken).ConfigureAwait(false);
            using var reader = ObjectReader.TryGetReader(stream, cancellationToken: cancellationToken);
            if (reader == null)
                return ImmutableArray<DiagnosticData>.Empty;

            if (!DiagnosticDataSerializer.TryReadDiagnosticData(reader, documentKey, cancellationToken, out diagnostics))
            {
                diagnostics = ImmutableArray<DiagnosticData>.Empty;
            }

            UpdateInMemoryCache(documentKey, checksum, diagnostics);
            return diagnostics;
        }

        private bool TryGetFromInMemoryCache(DocumentKey documentKey, Checksum checksum, out ImmutableArray<DiagnosticData> diagnostics)
        {
            lock (_cachedData)
            {
                var data = _cachedData.FirstOrNull(d => d.id == documentKey.Id && d.checksum == checksum);
                if (data != null)
                {
                    diagnostics = data.Value.diagnostics;
                    return true;
                }
            }

            diagnostics = default;
            return false;
        }

        private void UpdateInMemoryCache(DocumentKey documentKey, Checksum checksum, ImmutableArray<DiagnosticData> diagnostics)
        {
            lock (_cachedData)
            {
                // First, remove any existing info for this doc.
                for (var currentNode = _cachedData.First; currentNode != null; currentNode = currentNode.Next)
                {
                    if (currentNode.Value.id == documentKey.Id)
                    {
                        _cachedData.Remove(currentNode);
                        break;
                    }
                }

                // Then place the cached information for this doc at the end.
                _cachedData.AddLast((documentKey.Id, checksum, diagnostics));

                // And ensure we don't cache too many docs.
                if (_cachedData.Count > MaxCachedDocumentCount)
                    _cachedData.RemoveFirst();
            }
        }
    }
}
