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

                    var text = await TryGetSourceTextAsync().ConfigureAwait(false);
                    if (text == null)
                    {
                        // it won't bring in base text if it is not there already.
                        // text needed will be pulled in when there is request
                        return;
                    }

                    var newText = new SerializableSourceText(text.WithChanges(textChanges));
                    var newChecksum = serializer.CreateChecksum(newText, cancellationToken);

                    // save new text in the cache so that when asked, the data is most likely already there
                    //
                    // this cache is very short live. and new text created above is ChangedText which share
                    // text data with original text except the changes.
                    // so memory wise, this doesn't put too much pressure on the cache. it will not duplicates
                    // same text multiple times.
                    //
                    // also, once the changes are picked up and put into Workspace, normal Workspace 
                    // caching logic will take care of the text
                    WorkspaceManager.SolutionAssetCache.TryAddAsset(newChecksum, newText);
                }

                async Task<SourceText?> TryGetSourceTextAsync()
                {
                    // check the cheap and fast one first.
                    // see if the cache has the source text
                    if (WorkspaceManager.SolutionAssetCache.TryGetAsset<SerializableSourceText>(baseTextChecksum, out var serializableSourceText))
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

                    return await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }
    }
}
