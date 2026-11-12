// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;

[ExportCSharpVisualBasicStatelessLspService(typeof(ServiceBrokerConnectHandler)), Shared]
[Method("serviceBroker/connect")]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ServiceBrokerConnectHandler() : ILspServiceNotificationHandler<ServiceBrokerConnectHandler.NotificationParams>
{
    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    async Task INotificationHandler<NotificationParams, RequestContext>.HandleNotificationAsync(NotificationParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        var workspace = await requestContext.GetRequiredWorkspaceAsync(cancellationToken).ConfigureAwait(false);

        var serviceBrokerFactory = requestContext.GetRequiredService<ServiceBrokerFactory>();
        // Suppress logger async local context from flowing to the service broker connection.
        // This prevents all service broker requests from inheriting the LSP 'serviceBroker/connect' logging scope.
        // Suppression starts the work on a clean execution context, so re-establish the telemetry
        // instance there; it then flows to everything the connection spawns.
        var telemetry = RoslynTelemetry.Current;
        Task connectTask;
        using (ExecutionContext.SuppressFlow())
        {
            connectTask = Task.Run(async () =>
            {
                using var _ = RoslynTelemetry.SetCurrent(telemetry);
                await serviceBrokerFactory.CreateAndConnectAsync(request.PipeName, workspace).ConfigureAwait(false);
            }, CancellationToken.None);
        }

        await connectTask.ConfigureAwait(false);
    }

    private sealed class NotificationParams
    {
        [JsonPropertyName("pipeName")]
        public required string PipeName { get; set; }
    }
}
