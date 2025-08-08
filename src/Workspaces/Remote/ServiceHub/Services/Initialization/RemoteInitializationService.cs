// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

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
        var errorMessage = await RemoteExportProviderBuilder.InitializeAsync(localSettingsDirectory, cancellationToken).ConfigureAwait(false);

        var processId = await RunServiceAsync(cancellationToken =>
        {
            var service = (RemoteWorkspaceConfigurationService)GetWorkspaceServices().GetRequiredService<IWorkspaceConfigurationService>();
            service.InitializeOptions(options);

            return ValueTask.FromResult(Process.GetCurrentProcess().Id);
        }, cancellationToken).ConfigureAwait(false);

        return (processId, errorMessage);
    }
}
