// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

[ExportCSharpVisualBasicLspServiceFactory(typeof(OnInitializedService)), Shared]
internal sealed class OnInitializedServiceFactory : ILspServiceFactory
{
    private readonly IInitializationService? _initializationService;

    [ImportingConstructor]
    [Obsolete(StringConstants.ImportingConstructorMessage, error: true)]
    public OnInitializedServiceFactory([Import(AllowDefault = true)] IInitializationService? initializationService)
    {
        _initializationService = initializationService;
    }

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var clientLanguageServerManager = lspServices.GetRequiredService<IClientLanguageServerManager>();

        return new OnInitializedService(_initializationService, clientLanguageServerManager);
    }

    private class OnInitializedService : ILspService, IOnInitialized
    {
        private readonly IInitializationService? _initializationService;
        private readonly IClientLanguageServerManager _clientLanguageServerManager;

        public OnInitializedService(IInitializationService? initializationService, IClientLanguageServerManager clientLanguageServerManager)
        {
            _initializationService = initializationService;
            _clientLanguageServerManager = clientLanguageServerManager;
        }

        public async Task OnInitializedAsync(LSP.ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            if (_initializationService is null)
            {
                return;
            }

            await _initializationService.OnInitializedAsync(new ClientRequestManager(_clientLanguageServerManager), new ClientCapabilityProvider(clientCapabilities), cancellationToken).ConfigureAwait(false);
        }

        private class ClientRequestManager : IClientRequestManager
        {
            private readonly IClientLanguageServerManager _clientLanguageServerManager;

            public ClientRequestManager(IClientLanguageServerManager clientLanguageServerManager)
            {
                _clientLanguageServerManager = clientLanguageServerManager;
            }

            public Task<TResponse> SendRequestAsync<TParams, TResponse>(string methodName, TParams @params, CancellationToken cancellationToken)
                => _clientLanguageServerManager.SendRequestAsync<TParams, TResponse>(methodName, @params, cancellationToken);

            public ValueTask SendRequestAsync(string methodName, CancellationToken cancellationToken)
                => _clientLanguageServerManager.SendRequestAsync(methodName, cancellationToken);

            public ValueTask SendRequestAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken)
                => _clientLanguageServerManager.SendRequestAsync<TParams>(methodName, @params, cancellationToken);
        }
    }
}
