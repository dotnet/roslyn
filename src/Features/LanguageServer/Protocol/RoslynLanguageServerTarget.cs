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
        private IRequestDispatcher<RequestContext>? _requestDispatcher;
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

        private static IServiceCollection GetServiceCollection(
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
                .AddSingleton<IRequestExecutionQueue<RequestContext>>((serviceProvider) => new RoslynRequestExecutionQueue(serverKind, logger));

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
                //CreateLspServiceInstance<IClientLanguageServerManager>(),
                //CreateLspServiceInstance(logger),
                //CreateLspServiceInstance<IClientCapabilitiesProvider>(clientCapabilitiesProvider),
                //CreateLspServiceInstance<ICapabilitiesProvider>(capabilitiesProvider),
                //CreateLspServiceInstance<RoslynLifeCycleManager>(lifeCycleManager));

            return baseServices;

            static Lazy<ILspService, LspServiceMetadataView> CreateLspServiceInstance<T>(T lspServiceInstance) where T : ILspService
            {
                return new Lazy<ILspService, LspServiceMetadataView>(
                    () => lspServiceInstance, new LspServiceMetadataView(typeof(T)));
            }
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

        protected override IRequestExecutionQueue<RequestContext> GetRequestExecutionQueue()
        {
            var lspServices = GetLspServices();
            var requestExecutionQueue = new RoslynRequestExecutionQueue(_serverKind, _logger);
            requestExecutionQueue.Start(lspServices);

            return requestExecutionQueue;
        }

        protected override IRequestDispatcher<RequestContext> GetRequestDispatcher()
        {
            if (_requestDispatcher is null)
            {
                var lspServices = GetLspServices();
                _requestDispatcher = lspServices.GetRequiredService<RoslynRequestDispatcher>();

                SetupRequestDispatcher(_requestDispatcher);
            }

            return _requestDispatcher;
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

        internal TestAccessor GetTestAccessor()
        {
            throw new NotImplementedException();
        }

        internal class TestAccessor
        {
            internal void ExitServer()
            {
                throw new NotImplementedException();
            }

            //internal RoslynRequestExecutionQueue.TestAccessor GetQueueAccessor()
            //{
            //    throw new NotImplementedException();
            //}

            internal T GetRequiredLspService<T>() where T : class, ILspService
            {
                throw new NotImplementedException();
            }

            internal JsonRpc GetServerRpc()
            {
                throw new NotImplementedException();
            }

            internal bool HasShutdownStarted()
            {
                throw new NotImplementedException();
            }

            internal void ShutdownServer()
            {
                throw new NotImplementedException();
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
