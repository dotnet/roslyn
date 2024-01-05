// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// This service is used by the SolutionChecksumUpdater to proactively update the solution snapshot in the
    /// out-of-process workspace. We do this to limit the amount of time required to synchronize a solution over after
    /// an edit once a feature is asking for a snapshot.
    /// </summary>
    internal sealed class RemoteAssetSynchronizationService : BrokeredServiceBase, IRemoteAssetSynchronizationService
    {
        internal sealed class Factory : FactoryBase<IRemoteAssetSynchronizationService>
        {
            protected override IRemoteAssetSynchronizationService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteAssetSynchronizationService(in arguments);
        }

        public RemoteAssetSynchronizationService(in ServiceConstructionArguments arguments)
            : base(in arguments)
        {
        }

        public ValueTask SynchronizePrimaryWorkspaceAsync(Checksum solutionChecksum, int workspaceVersion, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_SynchronizePrimaryWorkspaceAsync, Checksum.GetChecksumLogInfo, solutionChecksum, cancellationToken))
                {
                    var workspace = GetWorkspace();
                    var assetProvider = workspace.CreateAssetProvider(solutionChecksum, WorkspaceManager.SolutionAssetCache, SolutionAssetSource);
                    await workspace.UpdatePrimaryBranchSolutionAsync(assetProvider, solutionChecksum, workspaceVersion, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public ValueTask SynchronizeTextAsync(DocumentId documentId, Checksum baseTextChecksum, IEnumerable<TextChange> textChanges, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var workspace = GetWorkspace();

                using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_SynchronizeTextAsync, Checksum.GetChecksumLogInfo, baseTextChecksum, cancellationToken))
                {
                    var serializer = workspace.Services.GetRequiredService<ISerializerService>();

                    // Try to get the text associated with baseTextChecksum
                    var text = await TryGetSourceTextAsync(WorkspaceManager, workspace, documentId, baseTextChecksum, cancellationToken).ConfigureAwait(false);
                    if (text == null)
                    {
                        // it won't bring in base text if it is not there already.
                        // text needed will be pulled in when there is request
                        return;
                    }

                    // Now attempt to manually apply the edit, producing the new forked text.  Store that directly in
                    // the asset cache so that future calls to retrieve it can do so quickly, without synchronizing over
                    // the entire document.
                    var newText = new SerializableSourceText(text.WithChanges(textChanges));
                    var newChecksum = serializer.CreateChecksum(newText, cancellationToken);

                    WorkspaceManager.SolutionAssetCache.GetOrAdd(newChecksum, newText);
                }

                return;

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
}
