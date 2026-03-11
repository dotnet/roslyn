// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteInitializationService(
    BrokeredServiceBase.ServiceConstructionArguments arguments)
    : BrokeredServiceBase(arguments), IRemoteInitializationService
{
    internal sealed class Factory : FactoryBase<IRemoteInitializationService>
    {
        protected override IRemoteInitializationService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteInitializationService(arguments);
    }

    public async ValueTask<(int processId, string? errorMessage)> InitializeAsync(WorkspaceConfigurationOptions options, string localSettingsDirectory, CancellationToken cancellationToken)
    {
        // Performed before RunServiceAsync to ensure that the export provider is initialized before the RemoteWorkspaceManager is created
        // as part of the RunServiceAsync call.
        var errorMessage = await RemoteExportProviderBuilder.InitializeAsync(localSettingsDirectory, TraceLogger, cancellationToken).ConfigureAwait(false);

        try
        {
            var processId = await RunServiceAsync(async cancellationToken =>
            {
                var service = (RemoteWorkspaceConfigurationService)GetWorkspaceServices().GetRequiredService<IWorkspaceConfigurationService>();
                service.InitializeOptions(options);

                return Process.GetCurrentProcess().Id;
            }, cancellationToken).ConfigureAwait(false);

            return (processId, errorMessage);
        }
        catch (Exception ex) when (errorMessage != null)
        {
            // We want to throw the exception but also include the message from the MEF creation
            throw new AggregateException(
                $"Error from {nameof(RemoteExportProviderBuilder)}: {errorMessage}",
                ex);
        }
    }
}
