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
    internal partial class RemoteProjectTelemetryService : ServiceBase, IRemoteProjectTelemetryService
    {
        internal sealed class Factory : FactoryBase
        {
            internal override WellKnownServiceHubService ServiceId
                => WellKnownServiceHubService.RemoteProjectTelemetryService;

            internal override object CreateService(IServiceProvider serviceProvider, IServiceBroker serviceBroker, ServiceActivationOptions serviceActivationOptions)
                => new RemoteProjectTelemetryService(serviceProvider, serviceBroker, (IProjectTelemetryListener)serviceActivationOptions.ClientRpcTarget!);
        }

        private readonly IProjectTelemetryListener _callback;

        public RemoteProjectTelemetryService(IServiceProvider serviceProvider, IServiceBroker serviceBroker, IProjectTelemetryListener callback)
            : base(serviceProvider, serviceBroker)
        {
            _callback = callback;
        }

        public Task ComputeProjectTelemetryAsync(CancellationToken cancellation)
        {
            return RunServiceAsync(() =>
            {
                var workspace = GetWorkspace();
                var endpoint = this.EndPoint;
                var registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
                var analyzerProvider = new RemoteProjectTelemetryIncrementalAnalyzerProvider(_callback);

                registrationService.AddAnalyzerProvider(
                    analyzerProvider,
                    new IncrementalAnalyzerProviderMetadata(
                        nameof(RemoteProjectTelemetryIncrementalAnalyzerProvider),
                        highPriorityForActiveFile: false,
                        workspaceKinds: WorkspaceKind.RemoteWorkspace));

                return Task.CompletedTask;
            }, cancellation);
        }
    }
}
