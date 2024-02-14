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
    private readonly IInitializationService _initializationService;

    [ImportingConstructor]
    [Obsolete(StringConstants.ImportingConstructorMessage, error: true)]
    public OnInitializedServiceFactory(IInitializationService initializationService)
    {
        _initializationService = initializationService;
    }

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        return new OnInitializedService(_initializationService);
    }

    private class OnInitializedService : ILspService, IOnInitialized
    {
        private readonly IInitializationService _initializationService;

        public OnInitializedService(IInitializationService initializationService)
        {
            _initializationService = initializationService;
        }

        public async Task OnInitializedAsync(LSP.ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            await _initializationService.OnInitializedAsync(new ClientCapabilityProvider(clientCapabilities), cancellationToken).ConfigureAwait(false);
        }
    }
}
