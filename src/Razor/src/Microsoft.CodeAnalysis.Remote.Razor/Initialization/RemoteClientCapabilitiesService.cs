// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.CodeAnalysis.Remote.Razor;

[Shared]
[Export(typeof(IClientCapabilitiesService))]
[Export(typeof(ILspLifetimeService))]
internal sealed class RemoteClientCapabilitiesService : AbstractClientCapabilitiesService, ILspLifetimeService
{
    public void OnLspInitialized(RemoteClientLSPInitializationOptions options)
    {
        SetCapabilities(options.ClientCapabilities);
    }
}
