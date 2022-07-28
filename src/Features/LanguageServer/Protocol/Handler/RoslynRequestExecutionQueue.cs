// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;

#nullable enable

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal class RequestContextFactory : IRequestContextFactory<RequestContext>
    {
        private readonly ImmutableArray<string> _supportedLanguages;
        private readonly ILspServices _lspServices;
        private readonly string _serverKind;

        public RequestContextFactory(ILspServices lspServices, ImmutableArray<string> supportedLanguages, string serverKind)
        {
            _lspServices = lspServices;
            _supportedLanguages = supportedLanguages;
            _serverKind = serverKind;
        }

        public Task<RequestContext?> CreateRequestContextAsync(IQueueItem<RequestContext> queueItem, CancellationToken queueCancellationToken, CancellationToken requestCancellationToken)
        {
            var clientCapabilitiesManager = _lspServices.GetRequiredService<IClientCapabilitiesManager>();
            var clientCapabilities = clientCapabilitiesManager.GetClientCapabilities();
            var logger = _lspServices.GetRequiredService<ILspLogger>();

            return RequestContext.CreateAsync(
                queueItem.RequiresLSPSolution,
                queueItem.MutatesSolutionState,
                queueItem.TextDocument,
                _serverKind,
                clientCapabilities,
                _supportedLanguages,
                _lspServices,
                logger,
                queueCancellationToken: queueCancellationToken,
                requestCancellationToken: requestCancellationToken);
        }
    }

    internal interface IClientCapabilitiesManager : ILspService
    {
        ClientCapabilities GetClientCapabilities();

        void SetClientCapabilities(ClientCapabilities clientCapabilities);
    }
}
