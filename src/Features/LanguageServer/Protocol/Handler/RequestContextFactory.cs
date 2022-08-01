// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;

#nullable enable

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal class RequestContextFactory : IRequestContextFactory<RequestContext>, ILspService
    {
        //private readonly IServerInfoProvider _serverInfoProvider;
        //private readonly ImmutableArray<string> _supportedLanguages;
        private readonly ILspServices _lspServices;
        //private readonly string _serverKind;

        public RequestContextFactory(ILspServices lspServices)//, IServerInfoProvider serverInfoProvider, ImmutableArray<string> supportedLanguages, string serverKind)
        {
            _lspServices = lspServices;

            //_supportedLanguages = supportedLanguages;
            //_serverKind = serverKind;
        }

        public Task<RequestContext?> CreateRequestContextAsync(IQueueItem<RequestContext> queueItem, CancellationToken queueCancellationToken, CancellationToken requestCancellationToken)
        {
            var clientCapabilitiesManager = _lspServices.GetRequiredService<IClientCapabilitiesManager>();
            var clientCapabilities = clientCapabilitiesManager.TryGetClientCapabilities();
            var logger = _lspServices.GetRequiredService<IRoslynLspLogger>();
            var serverInfoProvider = _lspServices.GetRequiredService<ServerInfoProvider>();

            return RequestContext.CreateAsync(
                queueItem.RequiresLSPSolution,
                queueItem.MutatesSolutionState,
                queueItem.TextDocument,
                serverInfoProvider.ServerKind,
                clientCapabilities,
                serverInfoProvider.SupportedLanguages,
                _lspServices,
                logger,
                queueCancellationToken: queueCancellationToken,
                requestCancellationToken: requestCancellationToken);
        }
    }
}
