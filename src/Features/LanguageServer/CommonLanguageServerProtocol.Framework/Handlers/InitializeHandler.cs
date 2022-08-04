// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace CommonLanguageServerProtocol.Framework.Handlers;

[LanguageServerEndpoint("initialize")]
public class InitializeHandler<RequestType, ResponseType, RequestContextType> 
    : IRequestHandler<RequestType, ResponseType, RequestContextType>
{
    private readonly IInitializeManager<RequestType, ResponseType> _capabilitiesManager;

    public InitializeHandler(IInitializeManager<RequestType, ResponseType> capabilitiesManager)
    {
        _capabilitiesManager = capabilitiesManager;
    }

    public bool MutatesSolutionState => true;

    public bool RequiresLSPSolution => false;

    public object? GetTextDocumentUri(RequestType request)
    {
        return null;
    }

    public Task<ResponseType> HandleRequestAsync(RequestType request, RequestContextType context, CancellationToken cancellationToken)
    {
        _capabilitiesManager.SetInitializeParams(request);

        var serverCapabilities = _capabilitiesManager.GetInitializeResult();

        return Task.FromResult(serverCapabilities);
    }
}
