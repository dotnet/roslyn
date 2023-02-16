// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Utilities.ServiceBroker;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;

[Export, Shared]
internal class MefServiceBroker : ServiceBrokerOfExportedServices
{
    private Task<GlobalBrokeredServiceContainer>? containerTask;

    [Import("PrivateBrokeredServiceContainer")]
    internal BrokeredServiceContainer BrokeredServiceContainer { get; private set; } = null!;

    protected override Task<GlobalBrokeredServiceContainer> GetBrokeredServiceContainerAsync(CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(BrokeredServiceContainer, $"{nameof(this.BrokeredServiceContainer)} must be set first.");
        this.containerTask ??= Task.FromResult((GlobalBrokeredServiceContainer)this.BrokeredServiceContainer);
        return this.containerTask;
    }
}
