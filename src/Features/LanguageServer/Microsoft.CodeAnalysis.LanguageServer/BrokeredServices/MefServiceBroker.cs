// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Utilities.ServiceBroker;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;

[Export, Shared]
internal class MefServiceBroker : ServiceBrokerOfExportedServices
{
    private readonly BrokeredServiceContainer _container;

    private Task<GlobalBrokeredServiceContainer>? containerTask;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public MefServiceBroker([Import("PrivateBrokeredServiceContainer")] BrokeredServiceContainer brokeredServiceContainer)
    {
        _container = brokeredServiceContainer;
    }

    protected override Task<GlobalBrokeredServiceContainer> GetBrokeredServiceContainerAsync(CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(_container, $"{nameof(_container)} must be set first.");
        this.containerTask ??= Task.FromResult((GlobalBrokeredServiceContainer)_container);
        return this.containerTask;
    }
}
