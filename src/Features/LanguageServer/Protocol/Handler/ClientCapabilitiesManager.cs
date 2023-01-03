// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// </summary>
/// <remarks>This is not actually stateless, but we need to be sure it doesn't re-construct each time it is retrieved 
/// and the only state will be wiped out on Server startup</remarks>
internal class ClientCapabilitiesManager : IClientCapabilitiesManager
{
    public ClientCapabilitiesManager()
    {
    }

    private ClientCapabilities? _clientCapabilities;

    public ClientCapabilities GetClientCapabilities()
    {
        if (_clientCapabilities is null)
        {
            throw new InvalidOperationException($"Tried to get required {nameof(ClientCapabilities)} before it was set");
        }

        return _clientCapabilities;
    }

    public void SetClientCapabilities(ClientCapabilities clientCapabilities)
    {
        _clientCapabilities = clientCapabilities;
    }

    public ClientCapabilities? TryGetClientCapabilities()
    {
        return _clientCapabilities;
    }
}
