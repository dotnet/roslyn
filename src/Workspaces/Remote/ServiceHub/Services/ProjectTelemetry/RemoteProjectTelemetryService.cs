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

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteProjectTelemetryService : ServiceBase, IRemoteProjectTelemetryService
    {
        public RemoteProjectTelemetryService(
            Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            StartService();
        }

        public Task ComputeProjectTelemetryAsync(CancellationToken cancellation)
        {
            return RunServiceAsync(() =>
            {
                var workspace = GetWorkspace();
                var endpoint = this.EndPoint;
                var registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
                var analyzerProvider = new RemoteProjectTelemetryIncrementalAnalyzerProvider(endpoint);

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
