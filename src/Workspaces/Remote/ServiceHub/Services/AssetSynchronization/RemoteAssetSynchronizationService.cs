// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// This service is used by the SolutionChecksumUpdater to proactively update the solution snapshot in the
/// out-of-process workspace. We do this to limit the amount of time required to synchronize a solution over after
/// an edit once a feature is asking for a snapshot.
/// </summary>
internal sealed class RemoteAssetSynchronizationService(in BrokeredServiceBase.ServiceConstructionArguments arguments)
    : BrokeredServiceBase(in arguments), IRemoteAssetSynchronizationService
{
    private const string SynchronizeTextChangesAsyncSucceededMetricName = "SucceededCount";
    private const string SynchronizeTextChangesAsyncFailedMetricName = "FailedCount";
    private const string SynchronizeTextChangesAsyncSucceededKeyName = nameof(RemoteAssetSynchronizationService) + "." + SynchronizeTextChangesAsyncSucceededMetricName;
    private const string SynchronizeTextChangesAsyncFailedKeyName = nameof(RemoteAssetSynchronizationService) + "." + SynchronizeTextChangesAsyncFailedMetricName;

    internal sealed class Factory : FactoryBase<IRemoteAssetSynchronizationService>
    {
        protected override IRemoteAssetSynchronizationService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteAssetSynchronizationService(in arguments);
    }

    public ValueTask SynchronizePrimaryWorkspaceAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
    {
        return RunServiceAsync(async cancellationToken =>
        {
            using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_SynchronizePrimaryWorkspaceAsync, Checksum.GetChecksumLogInfo, solutionChecksum, cancellationToken))
            {
                var workspace = GetWorkspace();
                var assetProvider = workspace.CreateAssetProvider(solutionChecksum, WorkspaceManager.SolutionAssetCache, SolutionAssetSource);
                await workspace.UpdatePrimaryBranchSolutionAsync(assetProvider, solutionChecksum, cancellationToken).ConfigureAwait(false);
            }
        }, cancellationToken);
    }

    public async ValueTask SynchronizeActiveDocumentAsync(DocumentId? documentId, CancellationToken cancellationToken)
    {
        var documentTrackingService = GetWorkspace().Services.GetRequiredService<IDocumentTrackingService>() as RemoteDocumentTrackingService;
        documentTrackingService?.SetActiveDocument(documentId);
    }

    public ValueTask SynchronizeTextChangesAsync(
        DocumentId documentId,
        Checksum baseTextChecksum,
        ImmutableArray<TextChange> textChanges,
        Checksum newTextChecksum,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(async cancellationToken =>
        {
            var wasSynchronized = await SynchronizeTextChangesHelperAsync().ConfigureAwait(false);

            var metricName = wasSynchronized ? SynchronizeTextChangesAsyncSucceededMetricName : SynchronizeTextChangesAsyncFailedMetricName;
            var keyName = wasSynchronized ? SynchronizeTextChangesAsyncSucceededKeyName : SynchronizeTextChangesAsyncFailedKeyName;
            TelemetryLogging.LogAggregatedCounter(FunctionId.RemoteHostService_SynchronizeTextAsyncStatus, KeyValueLogMessage.Create(static (m, args) =>
            {
                var (keyName, metricName) = args;
                m[TelemetryLogging.KeyName] = keyName;
                m[TelemetryLogging.KeyValue] = 1L;
                m[TelemetryLogging.KeyMetricName] = metricName;
            }, (keyName, metricName)));

            return;

            async Task<bool> SynchronizeTextChangesHelperAsync()
            {
                var workspace = GetWorkspace();

                using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_SynchronizeTextAsync, cancellationToken))
                {
                    // Try to get the text associated with baseTextChecksum
                    var text = await TryGetSourceTextAsync(WorkspaceManager, workspace, documentId, baseTextChecksum, cancellationToken).ConfigureAwait(false);
                    if (text == null)
                    {
                        // it won't bring in base text if it is not there already.
                        // text needed will be pulled in when there is request
                        return false;
                    }

                    // Now attempt to manually apply the edit, producing the new forked text.  Store that directly in
                    // the asset cache so that future calls to retrieve it can do so quickly, without synchronizing over
                    // the entire document.
                    var newText = text.WithChanges(textChanges);
                    var newSerializableText = new SerializableSourceText(newText, newTextChecksum);

                    WorkspaceManager.SolutionAssetCache.GetOrAdd(newSerializableText.ContentChecksum, newSerializableText);
                }

                return true;
            }

            async static Task<SourceText?> TryGetSourceTextAsync(
                RemoteWorkspaceManager workspaceManager,
                Workspace workspace,
                DocumentId documentId,
                Checksum baseTextChecksum,
                CancellationToken cancellationToken)
            {
                // check the cheap and fast one first.
                // see if the cache has the source text
                if (workspaceManager.SolutionAssetCache.TryGetAsset<SerializableSourceText>(baseTextChecksum, out var serializableSourceText))
                {
                    return await serializableSourceText.GetTextAsync(cancellationToken).ConfigureAwait(false);
                }

                // do slower one
                // check whether existing solution has it
                var document = workspace.CurrentSolution.GetDocument(documentId);
                if (document == null)
                {
                    return null;
                }

                // check checksum whether it is there.
                // since we lazily synchronize whole solution (SynchronizePrimaryWorkspaceAsync) when things are idle,
                // soon or later this will get hit even if text changes got out of sync due to issues in VS side
                // such as file is first opened and there is no SourceText in memory yet.
                if (!document.State.TryGetStateChecksums(out var state) ||
                    !state.Text.Equals(baseTextChecksum))
                {
                    return null;
                }

                return await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            }
        }, cancellationToken);
    }
}
