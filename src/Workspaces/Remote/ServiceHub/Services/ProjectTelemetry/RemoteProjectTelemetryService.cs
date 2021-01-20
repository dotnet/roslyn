// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ProjectTelemetry;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteProjectTelemetryService : BrokeredServiceBase, IRemoteProjectTelemetryService
    {
        internal sealed class Factory : FactoryBase<IRemoteProjectTelemetryService, IRemoteProjectTelemetryService.ICallback>
        {
            protected override IRemoteProjectTelemetryService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteProjectTelemetryService.ICallback> callback)
                => new RemoteProjectTelemetryService(arguments, callback);
        }

        private readonly RemoteCallback<IRemoteProjectTelemetryService.ICallback> _callback;

        public RemoteProjectTelemetryService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteProjectTelemetryService.ICallback> callback)
            : base(arguments)
        {
            _callback = callback;
        }

        public ValueTask ComputeProjectTelemetryAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                var workspace = GetWorkspace();
                var registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
                var analyzerProvider = new RemoteProjectTelemetryIncrementalAnalyzerProvider(_callback, callbackId);

                registrationService.AddAnalyzerProvider(
                    analyzerProvider,
                    new IncrementalAnalyzerProviderMetadata(
                        nameof(RemoteProjectTelemetryIncrementalAnalyzerProvider),
                        highPriorityForActiveFile: false,
                        workspaceKinds: WorkspaceKind.RemoteWorkspace));

                return default;
            }, cancellationToken);
        }
    }
}
