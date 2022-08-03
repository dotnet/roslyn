// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace CommonLanguageServerProtocol.Framework.Example;

[LanguageServerEndpoint(Methods.InitializeName)]
internal class InitializeHandler : IRequestHandler<InitializeParams, InitializeResult, ExampleRequestContext>
{
    public bool MutatesSolutionState => true;

    public bool RequiresLSPSolution => false;

    public object? GetTextDocumentUri(InitializeParams request)
    {
        return null;
    }

    public Task<InitializeResult> HandleRequestAsync(InitializeParams request, ExampleRequestContext context, CancellationToken cancellationToken)
    {
        var capabilities = request.Capabilities;

        var capabilitiesProvider = context.LspServices.GetRequiredService<ClientCapabilitiesProvider>();

        capabilitiesProvider.SetClientCapabilities(capabilities);

        var serverCapabilities = capabilitiesProvider.GetServerCapabilities();

        return Task.FromResult(new InitializeResult
        {
            Capabilities = serverCapabilities,
        });
    }
}
