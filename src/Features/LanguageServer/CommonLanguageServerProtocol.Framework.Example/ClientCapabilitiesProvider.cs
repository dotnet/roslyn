// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace CommonLanguageServerProtocol.Framework.Example;

internal class ClientCapabilitiesProvider
{
    private ClientCapabilities _clientCapabilities;

    public ServerCapabilities GetServerCapabilities()
    {
        return new ServerCapabilities()
        {
            SemanticTokensOptions = new SemanticTokensOptions
            {
                Range = true,
            },
        };
    }

    public void SetClientCapabilities(ClientCapabilities capabilities)
    {
        _clientCapabilities = capabilities;
    }
}
