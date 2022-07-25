// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class RoslynLanguageServerTarget : LanguageServerTarget<RequestContext>
    {
        private readonly IAsynchronousOperationListener _listener;
        private IAsyncToken? _erroredToken;

        public RoslynLanguageServerTarget(
            AbstractLspServiceProvider lspServiceProvider,
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            IAsynchronousOperationListenerProvider listenerProvider,
            IRoslynLspLogger logger,
            ImmutableArray<string> supportedLanguages,
            WellKnownLspServerKinds serverKind,
            ClientCapabilityProvider clientCapabilityProvider,
            ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> baseServices)
            : base(lspServiceProvider, jsonRpc, capabilitiesProvider, logger, supportedLanguages, serverKind.ToConvertableString(), clientCapabilityProvider)
        {
            _listener = listenerProvider.GetListener(FeatureAttribute.LanguageServer);
        }

        public override IRequestDispatcher<RequestContext> GetRequestDispatcher()
        {
            return new RequestDispatcher<RequestContext>(_lspServices);
        }

        public override IRequestExecutionQueue<RequestContext> GetRequestExecutionQueue(ImmutableArray<string> supportedlanguages, string serverKind, ILspServices lspServices)
        {
            return new RoslynRequestExecutionQueue(supportedlanguages, serverKind, lspServices, _logger);
        }

        protected override Task OnExitInternal()
        {
            return Task.CompletedTask;
        }

        public override ILspServices GetLspServiceProvider(
            ILspServiceProvider lspServiceProvider,
            string serverKind)
        {
            if (lspServiceProvider is not AbstractLspServiceProvider abstractLspServiceProvider)
            {
                throw new NotImplementedException($"{nameof(RoslynLanguageServerTarget)} needs an {nameof(ILspServiceProvider)} of type {nameof(AbstractLspServiceProvider)}");
            }

            if (_logger is IRoslynLspLogger roslynLogger && _clientCapabilitiesProvider is IRoslynClientCapabilitiesProvider roslynClientCapabilitiesProvider)
            {
                var baseServices = GetBaseServices(_jsonRpc, roslynLogger, roslynClientCapabilitiesProvider);
                abstractLspServiceProvider.SetBaseServices(baseServices);
            }
            else
            {
                throw new NotImplementedException("Roslyn types are not ILspServices");
            }

            // Add services that require base dependencies (jsonrpc) or are more complex to create to the set manually.
            return lspServiceProvider.CreateServices(serverKind);
        }

        internal static ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> GetBaseServices(JsonRpc jsonRpc, IRoslynLspLogger logger, IRoslynClientCapabilitiesProvider clientCapabilitiesProvider)
        {
            var baseServices = ImmutableArray.Create(
                CreateLspServiceInstance<IClientLanguageServerManager>(new ClientLanguageServerManager(jsonRpc)),
                CreateLspServiceInstance(logger),
                CreateLspServiceInstance(clientCapabilitiesProvider));

            return baseServices;

            static Lazy<ILspService, LspServiceMetadataView> CreateLspServiceInstance<T>(T lspServiceInstance) where T : ILspService
            {
                return new Lazy<ILspService, LspServiceMetadataView>(
                    () => lspServiceInstance, new LspServiceMetadataView(typeof(T)));
            }
        }

        public override void OnErroredStart()
        {
            _erroredToken = _listener.BeginAsyncOperation("RequestExecutionQueue_Errored");
        }

        public override Task OnErroredEndAsync(object obj)
        {
            _erroredToken?.Dispose();
            return Task.CompletedTask;
        }

        public override InitializeParams ClientSettings { get; }
    }

    internal interface IRoslynClientCapabilitiesProvider : IClientCapabilitiesProvider, ILspService
    {
    }

    internal interface IRoslynLspLogger : ILspLogger, ILspService
    {
    }

    internal class ClientCapabilityProvider : IRoslynClientCapabilitiesProvider
    {
        private ClientCapabilities? _clientCapabilities;

        public ClientCapabilityProvider()
        {
        }

        public void SetClientCapabilities(ClientCapabilities clientCapabilities)
        {
            _clientCapabilities = clientCapabilities;
        }

        public ClientCapabilities GetClientCapabilities()
        {
            Contract.ThrowIfNull(_clientCapabilities, $"InitializeAsync has not been called.");
            return _clientCapabilities;
        }

        public bool HasBeenSet()
        {
            return _clientCapabilities is not null;
        }
    }
}
