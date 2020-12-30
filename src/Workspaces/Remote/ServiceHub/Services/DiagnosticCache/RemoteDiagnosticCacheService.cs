// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        internal sealed class Factory : FactoryBase<IRemoteDiagnosticCacheService>
        {
            protected override IRemoteDiagnosticCacheService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteDiagnosticCacheService(arguments);
        }

        public RemoteDiagnosticCacheService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        private const string PersistenceName = "<CachedDiagnsoticsForDocument>";

        public ValueTask CacheDiagnosticsAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, ImmutableArray<DiagnosticData> diagnostics, CancellationToken cancellationToken)
            => RunServiceAsync(async cancellationToken =>
            {
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

            using var storage = persistenceService.GetStorage(solution);
            if (storage == null)
                return;

            using var stream = SerializableBytes.CreateWritableStream();
            using (var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken))
            {
                DiagnosticDataSerializer.WriteDiagnosticDataNoProjectInfo(writer, diagnostics, cancellationToken);
            }

            stream.Position = 0;

            var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);
            await storage.WriteStreamAsync(document, PersistenceName, stream, checksum, cancellationToken).ConfigureAwait(false);

            static async Task<Checksum> GetChecksumAsync(Document document, CancellationToken cancellationToken)
            {
                var checksums = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                return checksums.Text;
            }
        }

        private async Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(DocumentKey documentKey, Checksum checksum, CancellationToken cancellationToken)
        {
            var workspace = GetWorkspace();
            if (workspace.Services.GetService<IPersistentStorageService>() is not IChecksummedPersistentStorageService persistenceService)
                return ImmutableArray<DiagnosticData>.Empty;

            using var storage = persistenceService.GetStorage(workspace, documentKey.Project.Solution, checkBranchId: false);
            if (storage == null)
                return ImmutableArray<DiagnosticData>.Empty;

            using var stream = await storage.ReadStreamAsync(documentKey, PersistenceName, checksum, cancellationToken).ConfigureAwait(false);
            using var reader = ObjectReader.TryGetReader(stream, cancellationToken: cancellationToken);
            if (reader == null)
                return ImmutableArray<DiagnosticData>.Empty;

            return DiagnosticDataSerializer.TryReadDiagnosticData(reader, documentKey, cancellationToken, out var diagnostics)
                ? diagnostics
                : ImmutableArray<DiagnosticData>.Empty;
        }
    }
}
