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

[ExportCSharpVisualBasicLspService(typeof(OnInitializedService)), Shared(LspServiceComposition.SharingBoundary)]
internal sealed class OnInitializedService : ILspService, IOnInitialized
{
#pragma warning disable CS0618 // Type or member is obsolete
    private readonly IInitializationService? _initializationService;
#pragma warning restore CS0618 // Type or member is obsolete
    private readonly IOnInitializedService? _onInitializedService;
    private readonly LspService<IClientLanguageServerManager> _clientLanguageServerManager;

    [ImportingConstructor]
    [Obsolete(StringConstants.ImportingConstructorMessage, error: true)]
    public OnInitializedService(
        [Import(AllowDefault = true)] IInitializationService? initializationService,
        [Import(AllowDefault = true)] IOnInitializedService? onInitializedService,
        LspService<IClientLanguageServerManager> clientLanguageServerManager)
    {
        _initializationService = initializationService;
        _onInitializedService = onInitializedService;
        _clientLanguageServerManager = clientLanguageServerManager;
    }

    public async Task OnInitializedAsync(LSP.ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        if (_initializationService is not null)
        {
            await _initializationService.OnInitializedAsync(new ClientRequestManager(_clientLanguageServerManager.Value), new ClientCapabilityProvider(clientCapabilities), cancellationToken).ConfigureAwait(false);
        }

        if (_onInitializedService is not null)
        {
            await _onInitializedService.OnInitializedAsync(new ClientRequestManager(_clientLanguageServerManager.Value), clientCapabilities, cancellationToken).ConfigureAwait(false);
        }
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
