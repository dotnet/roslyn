// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteTodoCommentsService : BrokeredServiceBase, IRemoteTodoCommentsService
    {
        internal sealed class Factory : FactoryBase<ITodoCommentsListener>
        {
            protected override WellKnownServiceHubService ServiceId
                => WellKnownServiceHubService.RemoteTodoCommentsService;

            protected override object CreateService(IServiceProvider serviceProvider, IServiceBroker serviceBroker, RemoteCallback<ITodoCommentsListener> callback)
                => new RemoteTodoCommentsService(serviceProvider, serviceBroker, callback);
        }

        private readonly RemoteCallback<ITodoCommentsListener> _callback;

        public RemoteTodoCommentsService(IServiceProvider serviceProvider, IServiceBroker serviceBroker, RemoteCallback<ITodoCommentsListener> callback)
            : base(serviceProvider, serviceBroker, callback.ClientDisconnectedSource)
        {
            _callback = callback;
        }

        public ValueTask ComputeTodoCommentsAsync(CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                var workspace = GetWorkspace();
                var registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
                var analyzerProvider = new RemoteTodoCommentsIncrementalAnalyzerProvider(_callback);

                registrationService.AddAnalyzerProvider(
                    analyzerProvider,
                    new IncrementalAnalyzerProviderMetadata(
                        nameof(RemoteTodoCommentsIncrementalAnalyzerProvider),
                        highPriorityForActiveFile: false,
                        workspaceKinds: WorkspaceKind.RemoteWorkspace));

                return Task.CompletedTask;
            }, cancellationToken);
        }
    }
}
