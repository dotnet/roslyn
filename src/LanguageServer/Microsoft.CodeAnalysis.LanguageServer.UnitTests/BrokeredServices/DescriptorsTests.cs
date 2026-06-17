// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.BrokeredServices;

public sealed class DescriptorsTests
{
    [Fact]
    public void RemoteServicesToRegister_IncludesLegacyHotReloadServices()
    {
        Assert.Contains(new ServiceMoniker("Microsoft.VisualStudio.Debugger.ManagedHotReloadService", new(0, 1)), Descriptors.RemoteServicesToRegister.Keys);
        Assert.Contains(new ServiceMoniker("Microsoft.VisualStudio.Debugger.HotReloadLogger", new(0, 1)), Descriptors.RemoteServicesToRegister.Keys);
    }

    [Fact]
    public void RemoteServicesToRegister_IncludesHotReloadAgentServices()
    {
        Assert.Contains(new ServiceMoniker("Microsoft.VisualStudio.Debugger.HotReloadSessionNotificationService", new(0, 1)), Descriptors.RemoteServicesToRegister.Keys);
        Assert.Contains(new ServiceMoniker("Microsoft.VisualStudio.Debugger.ManagedHotReloadAgentManagerService", new(0, 1)), Descriptors.RemoteServicesToRegister.Keys);
        Assert.Contains(new ServiceMoniker("Microsoft.VisualStudio.Debugger.GenericHotReloadAgentManagerService", new(0, 1)), Descriptors.RemoteServicesToRegister.Keys);
        Assert.Contains(new ServiceMoniker("Microsoft.VisualStudio.HotReload.ProcessTrackingService", new(2, 0)), Descriptors.RemoteServicesToRegister.Keys);
        Assert.Contains(new ServiceMoniker("Microsoft.VisualStudio.HotReload.ProjectHotReloadSession", new(2, 0)), Descriptors.RemoteServicesToRegister.Keys);
    }
}
