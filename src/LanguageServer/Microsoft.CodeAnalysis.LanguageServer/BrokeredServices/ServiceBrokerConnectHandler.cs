// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Host.Mef;
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

    Task INotificationHandler<NotificationParams, RequestContext>.HandleNotificationAsync(NotificationParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        var workspace = requestContext.Workspace;
        Contract.ThrowIfNull(workspace, "We should always have a workspace since this is a solution-level handler.");

        var serviceBrokerFactory = requestContext.GetRequiredService<ServiceBrokerFactory>();
        // Suppress logger async local context from flowing to the service broker connection.
        // This prevents all service broker requests from inheriting the LSP 'serviceBroker/connect' logging scope.
        using (ExecutionContext.SuppressFlow())
        {
            return serviceBrokerFactory.CreateAndConnectAsync(request.PipeName, workspace);
        }
    }

    private sealed class NotificationParams
    {
        [JsonPropertyName("pipeName")]
        public required string PipeName { get; set; }
    }
}
