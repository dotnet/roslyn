// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services;
using Microsoft.VisualStudio.HotReload;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.BrokeredServices;

public sealed class DescriptorsTests
{
    [Fact]
    public void RemoteServicesToRegister_IncludesHotReloadServices()
    {
        Assert.Contains(IHotReloadEventSubscriber.ServiceDescriptor.Moniker, Descriptors.RemoteServicesToRegister.Keys);
        Assert.Contains(IManagedHotReloadUpdatesProviderRegistration.ServiceDescriptor.Moniker, Descriptors.RemoteServicesToRegister.Keys);
        Assert.Contains(IProcessTrackingService.ServiceDescriptor.Moniker, Descriptors.RemoteServicesToRegister.Keys);
        Assert.Contains(IRemoteProjectHotReloadSession.ServiceDescriptor.Moniker, Descriptors.RemoteServicesToRegister.Keys);
        Assert.Contains(IManagedHotReloadState.ServiceDescriptor.Moniker, Descriptors.RemoteServicesToRegister.Keys);
    }
}
