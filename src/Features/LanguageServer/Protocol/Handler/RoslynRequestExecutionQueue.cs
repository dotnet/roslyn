// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;

#nullable enable

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal class RoslynRequestExecutionQueue : RequestExecutionQueue<RequestContext>, ILspService
    {
        private ImmutableArray<string>? _supportedLanguages;
        private IRequestContextFactory<RequestContext>? _requestContextFactory;

        public RoslynRequestExecutionQueue(string serverKind, ILspServices services, ILspLogger logger) : base(serverKind, services, logger)
        {
        }

        public void SetSupportedLanguages(ImmutableArray<string> supportedLanguages)
        {
            _supportedLanguages = supportedLanguages;
        }

        public override IRequestContextFactory<RequestContext> GetRequestContextFactory(ILspServices lspServices)
        {
            if (_requestContextFactory is null)
            {
                if (_supportedLanguages is null)
                {
                    throw new ArgumentNullException(nameof(_supportedLanguages));
                }

                _requestContextFactory = new RequestContextFactory(lspServices, _supportedLanguages.Value, _serverKind);
            }

            return _requestContextFactory;
        }
    }

    internal class RequestContextFactory : IRequestContextFactory<RequestContext>, ILspService
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
            var clientCapabilities = clientCapabilitiesManager.TryGetClientCapabilities();
            var logger = _lspServices.GetRequiredService<IRoslynLspLogger>();

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

    /// <summary>
    /// </summary>
    /// <remarks>This is not actually stateless, but we need to be sure it doesn't re-construct each time it is retrieved 
    /// and the only state will be wiped out on Server startup</remarks>
    [ExportCSharpVisualBasicStatelessLspService(typeof(IClientCapabilitiesManager)), Shared]
    internal class ClientCapabilitiesManager : IClientCapabilitiesManager
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ClientCapabilitiesManager()
        {
        }

        private ClientCapabilities? _clientCapabilities;

        public ClientCapabilities GetClientCapabilities()
        {
            if (_clientCapabilities is null)
            {
                throw new InvalidOperationException($"Tried to get required {nameof(ClientCapabilities)} before it was set");
            }

            return _clientCapabilities;
        }

        public void SetClientCapabilities(ClientCapabilities clientCapabilities)
        {
            _clientCapabilities = clientCapabilities;
        }

        public ClientCapabilities? TryGetClientCapabilities()
        {
            return _clientCapabilities;
        }
    }

    internal interface IClientCapabilitiesManager : ILspService
    {
        ClientCapabilities GetClientCapabilities();

        ClientCapabilities? TryGetClientCapabilities();

        void SetClientCapabilities(ClientCapabilities clientCapabilities);
    }
}
