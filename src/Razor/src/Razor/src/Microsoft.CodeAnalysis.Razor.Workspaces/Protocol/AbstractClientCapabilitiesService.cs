// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.CodeAnalysis.Razor.Protocol;

internal abstract class AbstractClientCapabilitiesService : IClientCapabilitiesService
{
    private VSInternalClientCapabilities? _clientCapabilities;

    public bool CanGetClientCapabilities => _clientCapabilities is not null;

    public VSInternalClientCapabilities ClientCapabilities => _clientCapabilities ?? throw new InvalidOperationException("Client capabilities requested before initialized.");

    public void SetCapabilities(VSInternalClientCapabilities clientCapabilities)
    {
        _clientCapabilities = clientCapabilities;
    }
}
