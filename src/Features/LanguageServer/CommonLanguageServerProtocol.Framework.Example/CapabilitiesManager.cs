// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace CommonLanguageServerProtocol.Framework.Example;

internal class CapabilitiesManager : ICapabilitiesManager<InitializeParams, InitializeResult>
{
    private InitializeParams _initializeParams;

    public void SetClientCapabilities(InitializeParams request)
    {
        _initializeParams = request;
    }

    public InitializeResult GetServerCapabilities()
    {
        var serverCapabilities = new ServerCapabilities()
        {
            SemanticTokensOptions = new SemanticTokensOptions
            {
                Range = true,
            },
        };

        var initializeResult = new InitializeResult
        {
            Capabilities = serverCapabilities,
        };

        return initializeResult;
    }

    public InitializeParams GetClientCapabilities()
    {
        return _initializeParams;
    }
}
