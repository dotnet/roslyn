// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class RoslynLanguageServer : LanguageServer<RequestContext>, IClientCapabilitiesProvider
    {
        private readonly ICapabilitiesProvider _capabilitiesProvider;

        private readonly AbstractLspServiceProvider _lspServiceProvider;
        private readonly IAsynchronousOperationListener _listener;
        private IAsyncToken? _erroredToken;
        private readonly ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> _baseServices;
        private readonly IServiceCollection _serviceCollection;
        private readonly ImmutableArray<string> _supportedLanguages;
        private Task? _errorShutdownTask;
        private LspServices? _lspServices;

        public RoslynLanguageServer(
            AbstractLspServiceProvider lspServiceProvider,
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            IAsynchronousOperationListenerProvider listenerProvider,
            IRoslynLspLogger logger,
            ImmutableArray<string> supportedLanguages,
            WellKnownLspServerKinds serverKind)
            : base(jsonRpc, logger, serverKind.ToConvertableString())
        {
            _lspServiceProvider = lspServiceProvider;
            _listener = listenerProvider.GetListener(FeatureAttribute.LanguageServer);

            // Create services that require base dependencies (jsonrpc) or are more complex to create to the set manually.
            var lifeCycleManager = new RoslynLifeCycleManager(this);
            _baseServices = GetBaseServices(jsonRpc, this, logger, capabilitiesProvider, lifeCycleManager);
            _serviceCollection = GetServiceCollection(jsonRpc, this, logger, capabilitiesProvider, lifeCycleManager, serverKind.ToConvertableString(), supportedLanguages);
            _supportedLanguages = supportedLanguages;

            Initialize();
        }

        protected override ILspServices GetLspServices()
        {
            if (_lspServices is null)
            {
                _lspServices = _lspServiceProvider.CreateServices(_serverKind, _baseServices, _serviceCollection);
            }

            return _lspServices;
        }

        private IServiceCollection GetServiceCollection(
            JsonRpc jsonRpc,
            IClientCapabilitiesProvider clientCapabilitiesProvider,
            IRoslynLspLogger logger,
            ICapabilitiesProvider capabilitiesProvider,
            RoslynLifeCycleManager lifeCycleManager,
            string serverKind,
            ImmutableArray<string> supportedLanguages)
        {
            var serviceCollection = new ServiceCollection()
                .AddSingleton<IClientLanguageServerManager>(new ClientLanguageServerManager(jsonRpc))
                .AddSingleton<ILspLogger>(logger)
                .AddSingleton<IRoslynLspLogger>(logger)
                .AddSingleton<IClientCapabilitiesProvider>(clientCapabilitiesProvider)
                .AddSingleton<ICapabilitiesProvider>(capabilitiesProvider)
                .AddSingleton<ILifeCycleManager>(lifeCycleManager)
                .AddSingleton(new ServerInfoProvider(serverKind, supportedLanguages))
                .AddSingleton<IRequestContextFactory<RequestContext>, RequestContextFactory>()
                // TODO: Are these dangerous because of capturing?
                .AddSingleton<IRequestExecutionQueue<RequestContext>>((serviceProvider) => GetRequestExecutionQueue())
                .AddSingleton<IRequestDispatcher<RequestContext>>((serviceProvider) => GetRequestDispatcher())
                .AddSingleton<IClientCapabilitiesManager, ClientCapabilitiesManager>();

            return serviceCollection;
        }

        private static ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> GetBaseServices(
            JsonRpc jsonRpc,
            IClientCapabilitiesProvider clientCapabilitiesProvider,
            IRoslynLspLogger logger,
            ICapabilitiesProvider capabilitiesProvider,
            RoslynLifeCycleManager lifeCycleManager)
        {
            var baseServices = ImmutableArray.Create<Lazy<ILspService, LspServiceMetadataView>>();

            return baseServices;
        }

        protected override void RequestExecutionQueueErroredInternal(string message)
        {
            var messageParams = new LogMessageParams()
            {
                MessageType = MessageType.Error,
                Message = message
            };
            var lspServices = (LspServices)GetLspServices();
            var clientNotificationService = lspServices.GetRequiredLspService<IClientLanguageServerManager>();

            var asyncToken = _listener.BeginAsyncOperation("RequestExecutionQueue_Errored");
            _errorShutdownTask = Task.Run(async () =>
            {
                _logger?.TraceInformation("Shutting down language server.");

                await clientNotificationService.SendNotificationAsync("window/logMessage", message, CancellationToken.None).ConfigureAwait(false);

            }).CompletesAsyncOperation(asyncToken);
        }

        protected override IRequestDispatcher<RequestContext> ConstructDispatcher()
        {
            var lspServices = GetLspServices();
            var requestDispatcher = lspServices.GetRequiredService<RoslynRequestDispatcher>();

            SetupRequestDispatcher(requestDispatcher);

            return requestDispatcher;
        }

        protected override IRequestExecutionQueue<RequestContext> ConstructRequestExecutionQueue()
        {
            var queue = new RoslynRequestExecutionQueue(_serverKind, _logger);

            var lspServices = GetLspServices();
            queue.Start(lspServices);

            return queue;
        }

        public ClientCapabilities GetClientCapabilities()
        {
            var lspServices = GetLspServices();
            var clientCapabilitiesManager = lspServices.GetRequiredService<IClientCapabilitiesManager>();
            var clientCapabilities = clientCapabilitiesManager.GetClientCapabilities();

            return clientCapabilities;
        }

        protected override Task OnErroredEndAsync(object obj)
        {
            _erroredToken?.Dispose();
            return Task.CompletedTask;
        }

        internal TestAccessor GetTestAccessor() => new(this);

        internal class TestAccessor
        {
            private readonly RoslynLanguageServer _server;

            internal TestAccessor(RoslynLanguageServer server)
            {
                _server = server;
            }

            internal void ExitServer()
            {
                _server.Exit();
            }

            internal RoslynRequestExecutionQueue.TestAccessor GetQueueAccessor()
            {
                var queue = _server.GetRequestExecutionQueue();
                var concreteQueue = (RequestExecutionQueue<RequestContext>)queue;
                return concreteQueue.GetTestAccessor();
            }

            internal T GetRequiredLspService<T>() where T : class, ILspService
            {
                var lspServices = _server.GetLspServices();

                return lspServices.GetRequiredService<T>();
            }

            internal JsonRpc GetServerRpc()
            {
                return ((LanguageServer<RequestContext>)_server).GetTestAccessor().GetServerRpc();
            }

            internal bool HasShutdownStarted()
            {
                return _server.HasShutdownStarted;
            }

            internal void ShutdownServer()
            {
                _server.Shutdown();
            }
        }

        public override async ValueTask DisposeAsync()
        {
            // if the server shut down due to error, we might not have finished cleaning up
            if (_errorShutdownTask is not null)
                await _errorShutdownTask.ConfigureAwait(false);

            await base.DisposeAsync();
        }
    }
}
