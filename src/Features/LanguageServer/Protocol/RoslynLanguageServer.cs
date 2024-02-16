// Licensed to the .NET Foundation under one or more agreements.
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
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal sealed class RoslynLanguageServer : AbstractLanguageServer<RequestContext>, IOnInitialized
    {
        private readonly AbstractLspServiceProvider _lspServiceProvider;
        private readonly ImmutableDictionary<Type, ImmutableArray<Func<ILspServices, object>>> _baseServices;
        private readonly WellKnownLspServerKinds _serverKind;

        public RoslynLanguageServer(
            AbstractLspServiceProvider lspServiceProvider,
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            AbstractLspLogger logger,
            HostServices hostServices,
            ImmutableArray<string> supportedLanguages,
            WellKnownLspServerKinds serverKind)
            : base(jsonRpc, logger)
        {
            _lspServiceProvider = lspServiceProvider;
            _serverKind = serverKind;

            // Create services that require base dependencies (jsonrpc) or are more complex to create to the set manually.
            _baseServices = GetBaseServices(jsonRpc, logger, capabilitiesProvider, hostServices, serverKind, supportedLanguages);

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
            return provider.CreateRequestExecutionQueue(this, _logger, HandlerProvider);
        }

        private ImmutableDictionary<Type, ImmutableArray<Func<ILspServices, object>>> GetBaseServices(
            JsonRpc jsonRpc,
            AbstractLspLogger logger,
            ICapabilitiesProvider capabilitiesProvider,
            HostServices hostServices,
            WellKnownLspServerKinds serverKind,
            ImmutableArray<string> supportedLanguages)
        {
            var baseServices = new Dictionary<Type, ImmutableArray<Func<ILspServices, object>>>();
            var clientLanguageServerManager = new ClientLanguageServerManager(jsonRpc);
            var lifeCycleManager = new LspServiceLifeCycleManager(clientLanguageServerManager);

            AddBaseService<IClientLanguageServerManager>(clientLanguageServerManager);
            AddBaseService<ILspLogger>(logger);
            AddBaseService<AbstractLspLogger>(logger);
            AddBaseService<ICapabilitiesProvider>(capabilitiesProvider);
            AddBaseService<ILifeCycleManager>(lifeCycleManager);
            AddBaseService(new ServerInfoProvider(serverKind, supportedLanguages));
            AddBaseServiceFromFunc<AbstractRequestContextFactory<RequestContext>>((lspServices) => new RequestContextFactory(lspServices));
            AddBaseServiceFromFunc<IRequestExecutionQueue<RequestContext>>((_) => GetRequestExecutionQueue());
            AddBaseServiceFromFunc<AbstractTelemetryService>((lspServices) => new TelemetryService(lspServices));
            AddBaseService<IInitializeManager>(new InitializeManager());
            AddBaseService<IMethodHandler>(new InitializeHandler());
            AddBaseService<IMethodHandler>(new InitializedHandler());
            AddBaseService<IOnInitialized>(this);
            AddBaseService<ILanguageInfoProvider>(new LanguageInfoProvider());

            // In all VS cases, we already have a misc workspace.  Specifically
            // Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.MiscellaneousFilesWorkspace.  In
            // those cases, we do not need to add an additional workspace to manage new files we hear about.  So only
            // add the LspMiscellaneousFilesWorkspace for hosts that have not already brought their own.
            if (serverKind == WellKnownLspServerKinds.CSharpVisualBasicLspServer)
                AddBaseServiceFromFunc<LspMiscellaneousFilesWorkspace>(lspServices => new LspMiscellaneousFilesWorkspace(lspServices, hostServices));

            return baseServices.ToImmutableDictionary();

            void AddBaseService<T>(T instance) where T : class
            {
                AddBaseServiceFromFunc<T>((_) => instance);
            }

            void AddBaseServiceFromFunc<T>(Func<ILspServices, object> creatorFunc)
            {
                var added = baseServices.GetValueOrDefault(typeof(T), []).Add(creatorFunc);
                baseServices[typeof(T)] = added;
            }
        }

        public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            OnInitialized();
            return Task.CompletedTask;
        }
    }
}
