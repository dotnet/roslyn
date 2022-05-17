// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.TodoComments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteTodoCommentsDiscoveryService : BrokeredServiceBase, IRemoteTodoCommentsDiscoveryService
    {
        internal sealed class Factory : FactoryBase<IRemoteTodoCommentsDiscoveryService, IRemoteTodoCommentsDiscoveryService.ICallback>
        {
            protected override IRemoteTodoCommentsDiscoveryService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteTodoCommentsDiscoveryService.ICallback> callback)
                => new RemoteTodoCommentsDiscoveryService(arguments, callback);
        }

        private readonly RemoteCallback<IRemoteTodoCommentsDiscoveryService.ICallback> _callback;

        private RemoteTodoCommentsIncrementalAnalyzer? _lazyAnalyzer;

        public RemoteTodoCommentsDiscoveryService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteTodoCommentsDiscoveryService.ICallback> callback)
            : base(arguments)
        {
            _callback = callback;
        }

        public ValueTask ComputeTodoCommentsAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                var workspace = GetWorkspace();
                var registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();

                // This method should only be called once.
                Contract.ThrowIfFalse(Interlocked.Exchange(ref _lazyAnalyzer, new RemoteTodoCommentsIncrementalAnalyzer(_callback, callbackId)) == null);

                registrationService.AddAnalyzerProvider(
                    new RemoteTodoCommentsIncrementalAnalyzerProvider(_lazyAnalyzer),
                    new IncrementalAnalyzerProviderMetadata(
                        nameof(RemoteTodoCommentsIncrementalAnalyzerProvider),
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
    }
}
