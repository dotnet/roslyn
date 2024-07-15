// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Contracts.Client;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal sealed class SolutionSnapshotProviderProxy(IServiceBroker serviceBroker) :
    BrokeredServiceProxy<ISolutionSnapshotProvider>(serviceBroker, BrokeredServiceDescriptors.SolutionSnapshotProvider),
    ISolutionSnapshotProvider
{
    public ValueTask<SolutionSnapshotId> RegisterSolutionSnapshotAsync(CancellationToken cancellationToken)
        => InvokeAsync((service, cancellationToken) => service.RegisterSolutionSnapshotAsync(cancellationToken), cancellationToken);
}
