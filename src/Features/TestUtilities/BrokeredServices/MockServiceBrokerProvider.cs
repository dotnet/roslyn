// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.BrokeredServices.UnitTests;

[Export(typeof(IServiceBrokerProvider)), PartNotDiscoverable, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class MockServiceBrokerProvider() : IServiceBrokerProvider
{
    private readonly IServiceBroker _serviceBroker = new MockServiceBroker();
    public IServiceBroker ServiceBroker => throw new InvalidOperationException("Use GetServiceBrokerAsync instead.");
    public Task<IServiceBroker> GetServiceBrokerAsync(CancellationToken cancellationToken) => Task.FromResult(_serviceBroker);
}
