﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.ServerLifetime;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal sealed class RoslynLanguageServer : AbstractLanguageServer<RequestContext>, IClientCapabilitiesProvider, IOnInitialized
    {
        private readonly AbstractLspServiceProvider _lspServiceProvider;
        private readonly ImmutableDictionary<Type, ImmutableArray<Func<ILspServices, object>>> _baseServices;
        private readonly WellKnownLspServerKinds _serverKind;

        public RoslynLanguageServer(
            AbstractLspServiceProvider lspServiceProvider,
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            ILspServiceLogger logger,
            HostServices hostServices,
            ImmutableArray<string> supportedLanguages,
            WellKnownLspServerKinds serverKind)
            : base(jsonRpc, logger)
        {
            _lspServiceProvider = lspServiceProvider;
            _serverKind = serverKind;

            // Create services that require base dependencies (jsonrpc) or are more complex to create to the set manually.
            _baseServices = GetBaseServices(jsonRpc, this, logger, capabilitiesProvider, hostServices, serverKind, supportedLanguages);

            // This spins up the queue and ensure the LSP is ready to start receiving requests
            Initialize();
        }

        protected override ILspServices ConstructLspServices()
        {
            return _lspServiceProvider.CreateServices(_serverKind, _baseServices);
        }

        protected override IRequestExecutionQueue<RequestContext> ConstructRequestExecutionQueue()
        {
            var provider = GetLspServices().GetRequiredService<IRequestExecutionQueueProvider<RequestContext>>();
            return provider.CreateRequestExecutionQueue(this, _logger, GetHandlerProvider());
        }

        private ImmutableDictionary<Type, ImmutableArray<Func<ILspServices, object>>> GetBaseServices(
            JsonRpc jsonRpc,
            IClientCapabilitiesProvider clientCapabilitiesProvider,
            ILspServiceLogger logger,
            ICapabilitiesProvider capabilitiesProvider,
            HostServices hostServices,
            WellKnownLspServerKinds serverKind,
            ImmutableArray<string> supportedLanguages)
        {
            var baseServices = new Dictionary<Type, ImmutableArray<Func<ILspServices, object>>>();
            var clientLanguageServerManager = new ClientLanguageServerManager(jsonRpc);
            var lifeCycleManager = new LspServiceLifeCycleManager(clientLanguageServerManager);

            AddBaseService<LspMiscellaneousFilesWorkspace>(new LspMiscellaneousFilesWorkspace(hostServices));
            AddBaseService<IClientLanguageServerManager>(clientLanguageServerManager);
            AddBaseService<ILspLogger>(logger);
            AddBaseService<ILspServiceLogger>(logger);
            AddBaseService<IClientCapabilitiesProvider>(clientCapabilitiesProvider);
            AddBaseService<ICapabilitiesProvider>(capabilitiesProvider);
            AddBaseService<ILifeCycleManager>(lifeCycleManager);
            AddBaseService(new ServerInfoProvider(serverKind, supportedLanguages));
            AddBaseServiceFromFunc<IRequestContextFactory<RequestContext>>((lspServices) => new RequestContextFactory(lspServices));
            AddBaseServiceFromFunc<IRequestExecutionQueue<RequestContext>>((_) => GetRequestExecutionQueue());
            AddBaseService<IClientCapabilitiesManager>(new ClientCapabilitiesManager());
            AddBaseService<IMethodHandler>(new InitializeHandler());
            AddBaseService<IMethodHandler>(new InitializedHandler());
            AddBaseService<IOnInitialized>(this);

            return baseServices.ToImmutableDictionary();

            void AddBaseService<T>(T instance) where T : class
            {
                AddBaseServiceFromFunc<T>((_) => instance);
            }

            void AddBaseServiceFromFunc<T>(Func<ILspServices, object> creatorFunc)
            {
                var added = baseServices.GetValueOrDefault(typeof(T), ImmutableArray<Func<ILspServices, object>>.Empty).Add(creatorFunc);
                baseServices[typeof(T)] = added;
            }
        }

        public ClientCapabilities GetClientCapabilities()
        {
            var lspServices = GetLspServices();
            var clientCapabilitiesManager = lspServices.GetRequiredService<IClientCapabilitiesManager>();
            var clientCapabilities = clientCapabilitiesManager.GetClientCapabilities();

            return clientCapabilities;
        }

        public Task OnInitializedAsync(ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            OnInitialized();
            return Task.CompletedTask;
        }
    }
}
