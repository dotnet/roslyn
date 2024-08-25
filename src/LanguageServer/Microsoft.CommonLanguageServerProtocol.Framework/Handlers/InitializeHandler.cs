// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework.Handlers;

[LanguageServerEndpoint("initialize", LanguageServerConstants.DefaultLanguageName)]
internal class InitializeHandler<TRequest, TResponse, TRequestContext>
    : IRequestHandler<TRequest, TResponse, TRequestContext>
{
    private readonly IInitializeManager<TRequest, TResponse> _capabilitiesManager;

    public InitializeHandler(IInitializeManager<TRequest, TResponse> capabilitiesManager)
    {
        _capabilitiesManager = capabilitiesManager;
    }

    public bool MutatesSolutionState => true;

    public Task<TResponse> HandleRequestAsync(TRequest request, TRequestContext context, CancellationToken cancellationToken)
    {
        _capabilitiesManager.SetInitializeParams(request);

        var serverCapabilities = _capabilitiesManager.GetInitializeResult();

        return Task.FromResult(serverCapabilities);
    }
}
