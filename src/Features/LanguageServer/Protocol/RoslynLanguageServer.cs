// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.ServerLifetime;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class RoslynLanguageServer : AbstractLanguageServer<RequestContext>, IClientCapabilitiesProvider
    {
        private readonly AbstractLspServiceProvider _lspServiceProvider;
        private readonly ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> _baseServices;
        private readonly IServiceCollection _serviceCollection;
        private readonly WellKnownLspServerKinds _serverKind;

        public RoslynLanguageServer(
            AbstractLspServiceProvider lspServiceProvider,
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            ILspServiceLogger logger,
            ImmutableArray<string> supportedLanguages,
            WellKnownLspServerKinds serverKind)
            : base(jsonRpc, logger)
        {
            _lspServiceProvider = lspServiceProvider;
            _serverKind = serverKind;

            // Create services that require base dependencies (jsonrpc) or are more complex to create to the set manually.
            _baseServices = GetBaseServices();
            _serviceCollection = GetServiceCollection(jsonRpc, this, logger, capabilitiesProvider, serverKind, supportedLanguages);

            // This spins up the queue and ensure the LSP is ready to start receiving requests
            Initialize();
        }

        protected override ILspServices ConstructLspServices()
        {
            return _lspServiceProvider.CreateServices(_serverKind, _baseServices, _serviceCollection);
        }

        protected override IRequestExecutionQueue<RequestContext> ConstructRequestExecutionQueue()
        {
            var handlerProvider = GetHandlerProvider();
            var queue = new RoslynRequestExecutionQueue(_logger, handlerProvider);

            queue.Start();
            return queue;
        }

        private IServiceCollection GetServiceCollection(
            JsonRpc jsonRpc,
            IClientCapabilitiesProvider clientCapabilitiesProvider,
            ILspServiceLogger logger,
            ICapabilitiesProvider capabilitiesProvider,
            WellKnownLspServerKinds serverKind,
            ImmutableArray<string> supportedLanguages)
        {
            var clientLanguageServerManager = new ClientLanguageServerManager(jsonRpc);
            var lifeCycleManager = new LspServiceLifeCycleManager(this, logger, clientLanguageServerManager);

            var serviceCollection = new ServiceCollection()
                .AddSingleton<IClientLanguageServerManager>(clientLanguageServerManager)
                .AddSingleton<ILspLogger>(logger)
                .AddSingleton<ILspServiceLogger>(logger)
                .AddSingleton<IClientCapabilitiesProvider>(clientCapabilitiesProvider)
                .AddSingleton<ICapabilitiesProvider>(capabilitiesProvider)
                .AddSingleton<ILifeCycleManager>(lifeCycleManager)
                .AddSingleton(new ServerInfoProvider(serverKind, supportedLanguages))
                .AddSingleton<IRequestContextFactory<RequestContext>, RequestContextFactory>()
                .AddSingleton<IRequestExecutionQueue<RequestContext>>((serviceProvider) => GetRequestExecutionQueue())
                .AddSingleton<IClientCapabilitiesManager, ClientCapabilitiesManager>();
            AddHandler<InitializeHandler>(serviceCollection);
            AddHandler<InitializedHandler>(serviceCollection);
            AddHandler<ShutdownHandler>(serviceCollection);
            AddHandler<ExitHandler>(serviceCollection);

            return serviceCollection;
        }

        private static void AddHandler<THandler>(IServiceCollection serviceCollection) where THandler : class, IMethodHandler
        {
            _ = serviceCollection.AddSingleton<IMethodHandler, THandler>();
        }

        private static ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> GetBaseServices()
        {
            var baseServices = ImmutableArray.Create<Lazy<ILspService, LspServiceMetadataView>>();

            return baseServices;
        }

        public ClientCapabilities GetClientCapabilities()
        {
            var lspServices = GetLspServices();
            var clientCapabilitiesManager = lspServices.GetRequiredService<IClientCapabilitiesManager>();
            var clientCapabilities = clientCapabilitiesManager.GetClientCapabilities();

            return clientCapabilities;
        }
    }
}
