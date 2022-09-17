// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.TaskList;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteTaskListService : BrokeredServiceBase, IRemoteTaskListService
    {
        internal sealed class Factory : FactoryBase<IRemoteTaskListService, IRemoteTaskListService.ICallback>
        {
            protected override IRemoteTaskListService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteTaskListService.ICallback> callback)
                => new RemoteTaskListService(arguments, callback);
        }

        private readonly RemoteCallback<IRemoteTaskListService.ICallback> _callback;

        private RemoteTaskListIncrementalAnalyzer? _lazyAnalyzer;

        public RemoteTaskListService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteTaskListService.ICallback> callback)
            : base(arguments)
        {
            _callback = callback;
        }

        public ValueTask ComputeTaskListItemsAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                var workspace = GetWorkspace();
                var registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();

                // This method should only be called once.
                Contract.ThrowIfFalse(Interlocked.Exchange(ref _lazyAnalyzer, new RemoteTaskListIncrementalAnalyzer(_callback, callbackId)) == null);

                registrationService.AddAnalyzerProvider(
                    new RemoteTaskListIncrementalAnalyzerProvider(_lazyAnalyzer),
                    new IncrementalAnalyzerProviderMetadata(
                        nameof(RemoteTaskListIncrementalAnalyzerProvider),
                        highPriorityForActiveFile: false,
                        workspaceKinds: WorkspaceKind.RemoteWorkspace));

                return ValueTaskFactory.CompletedTask;
            }, cancellationToken);
        }

        public ValueTask ReanalyzeAsync(CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                if (_lazyAnalyzer == null)
                {
                    // ComputeTodoCommentsAsync hasn't been called yet
                    return ValueTaskFactory.CompletedTask;
                }

                var workspace = GetWorkspace();
                var registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerService>();
                registrationService.Reanalyze(workspace, _lazyAnalyzer, projectIds: null, documentIds: null, highPriority: false);

                return ValueTaskFactory.CompletedTask;
            }, cancellationToken);
        }

        public ValueTask<ImmutableArray<TaskListItem>> GetTaskListItemsAsync(
            Checksum solutionChecksum, DocumentId documentId, ImmutableArray<TaskListItemDescriptor> descriptors, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var document = await solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                var service = document.GetRequiredLanguageService<ITaskListService>();
                return await service.GetTaskListItemsAsync(document, descriptors, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }
    }
}
