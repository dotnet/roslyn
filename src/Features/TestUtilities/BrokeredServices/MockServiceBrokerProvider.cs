// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.BrokeredServices.UnitTests;

[Export(typeof(IServiceBrokerProvider)), PartNotDiscoverable, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class MockServiceBrokerProvider() : IServiceBrokerProvider
{
    public IServiceBroker ServiceBroker { get; } = new MockServiceBroker();
}
