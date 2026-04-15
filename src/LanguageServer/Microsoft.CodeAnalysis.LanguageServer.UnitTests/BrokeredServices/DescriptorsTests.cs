// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.BrokeredServices;

public sealed class DescriptorsTests
{
    [Fact]
    public void RemoteServicesToRegister_IncludesLegacyHotReloadServices()
    {
        Assert.Contains(BrokeredServiceDescriptors.DebuggerManagedHotReloadServiceLegacy.Moniker, Descriptors.RemoteServicesToRegister.Keys);
        Assert.Contains(BrokeredServiceDescriptors.HotReloadLoggerServiceLegacy.Moniker, Descriptors.RemoteServicesToRegister.Keys);
    }
}
