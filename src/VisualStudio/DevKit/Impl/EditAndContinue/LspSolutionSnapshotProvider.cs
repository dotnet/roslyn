// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Contracts.Client;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal sealed class LspSolutionSnapshotProvider(
    IServiceBroker serviceBroker,
    SolutionSnapshotRegistry solutionSnapshotRegistry)
    : BrokeredServiceProxy<ISolutionSnapshotProviderService>(serviceBroker, BrokeredServiceDescriptors.SolutionSnapshotProvider),
      ISolutionSnapshotProvider,
      IDisposable
{
    public void Dispose()
    {
        solutionSnapshotRegistry.Clear();
    }

    public async ValueTask<Solution> GetCurrentSolutionAsync(CancellationToken cancellationToken)
    {
        // First, calls to the client to get the current snapshot id.
        // The client service calls the LSP client, which sends message to the LSP server, which in turn calls back to RegisterSolutionSnapshot.
        // Once complete the snapshot should be registered.
        var id = await InvokeAsync((service, cancellationToken) => service.RegisterSolutionSnapshotAsync(cancellationToken), cancellationToken).ConfigureAwait(false);

        return solutionSnapshotRegistry.GetRegisteredSolutionSnapshot(id);
    }
}
