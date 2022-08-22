// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.oft.CodeAnalysis.LanguageServer
{
    internal class RoslynLanguageServer : AbstractLanguageServer<RequestContext>, IClientCapabilitiesProvider
    {
        private readonly AbstractLspServiceProvider _lspServiceProvider;
        private readonly IAsynchronousOperationListener _listener;
        private readonly ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> _baseServices;
        private readonly IServiceCollection _serviceCollection;
        private Task? _errorShutdownTask;
        private readonly string _serverKind;

        public RoslynLanguageServer(
            AbstractLspServiceProvider lspServiceProvider,
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            IAsynchronousOperationListenerProvider listenerProvider,
            IRoslynLspLogger logger,
            ImmutableArray<string> supportedLanguages,
            WellKnownLspServerKinds serverKind)
            : base(jsonRpc, logger)
        {
            _lspServiceProvider = lspServiceProvider;
            _listener = listenerProvider.GetListener(FeatureAttribute.LanguageServer);
            _serverKind = serverKind.ToConvertableString();

            // Create services that require base dependencies (jsonrpc) or are more complex to create to the set manually.
            var lifeCycleManager = new RoslynLifeCycleManager(this);
            _baseServices = GetBaseServices();
            _serviceCollection = GetServiceCollection(jsonRpc, this, logger, capabilitiesProvider, lifeCycleManager, serverKind.ToConvertableString(), supportedLanguages);
        }

        protected override ILspServices ConstructLspServices()
        {
            return _lspServiceProvider.CreateServices(_serverKind, _baseServices, _serviceCollection);
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
                .AddSingleton<LifeCycleManager<RequestContext>>(lifeCycleManager)
                .AddSingleton(new ServerInfoProvider(serverKind, supportedLanguages))
                .AddSingleton<IRequestContextFactory<RequestContext>, RequestContextFactory>()
                .AddSingleton<IRequestExecutionQueue<RequestContext>>((serviceProvider) => GetRequestExecutionQueue())
                .AddSingleton<IClientCapabilitiesManager, ClientCapabilitiesManager>();

            return serviceCollection;
        }

        private static ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> GetBaseServices()
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
                await _logger.LogInformationAsync("Shutting down language server.", CancellationToken.None).ConfigureAwait(false);

                await clientNotificationService.SendNotificationAsync("window/logMessage", message, CancellationToken.None).ConfigureAwait(false);

            }).CompletesAsyncOperation(asyncToken);
        }

        public ClientCapabilities GetClientCapabilities()
        {
            var lspServices = GetLspServices();
            var clientCapabilitiesManager = lspServices.GetRequiredService<IClientCapabilitiesManager>();
            var clientCapabilities = clientCapabilitiesManager.GetClientCapabilities();

            return clientCapabilities;
        }

        public override async ValueTask DisposeAsync()
        {
            // if the server shut down due to error, we might not have finished cleaning up
            if (_errorShutdownTask is not null)
                await _errorShutdownTask.ConfigureAwait(false);

            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}
