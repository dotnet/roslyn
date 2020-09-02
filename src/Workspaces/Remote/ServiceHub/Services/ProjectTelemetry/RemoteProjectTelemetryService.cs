// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ProjectTelemetry;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteProjectTelemetryService : BrokeredServiceBase, IRemoteProjectTelemetryService
    {
        internal sealed class Factory : FactoryBase<IProjectTelemetryListener>
        {
            protected override WellKnownServiceHubService ServiceId
                => WellKnownServiceHubService.RemoteProjectTelemetryService;

            protected override object CreateService(IServiceProvider serviceProvider, IServiceBroker serviceBroker, RemoteCallback<IProjectTelemetryListener> callback)
                => new RemoteProjectTelemetryService(serviceProvider, serviceBroker, callback);
        }

        private readonly RemoteCallback<IProjectTelemetryListener> _callback;

        public RemoteProjectTelemetryService(IServiceProvider serviceProvider, IServiceBroker serviceBroker, RemoteCallback<IProjectTelemetryListener> callback)
            : base(serviceProvider, serviceBroker, callback.ClientDisconnectedSource)
        {
            _callback = callback;
        }

        public ValueTask ComputeProjectTelemetryAsync(CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                var workspace = GetWorkspace();
                var registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
                var analyzerProvider = new RemoteProjectTelemetryIncrementalAnalyzerProvider(_callback);

                registrationService.AddAnalyzerProvider(
                    analyzerProvider,
                    new IncrementalAnalyzerProviderMetadata(
                        nameof(RemoteProjectTelemetryIncrementalAnalyzerProvider),
                        highPriorityForActiveFile: false,
                        workspaceKinds: WorkspaceKind.RemoteWorkspace));

                return Task.CompletedTask;
            }, cancellationToken);
        }
    }
}
