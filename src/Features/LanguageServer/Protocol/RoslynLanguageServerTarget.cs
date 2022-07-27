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
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.RpcContracts;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class RoslynLanguageServerTarget : LanguageServerTarget<RequestContext>
    {
        private readonly AbstractLspServiceProvider _lspServiceProvider;
        private readonly IAsynchronousOperationListener _listener;
        private IAsyncToken? _erroredToken;
        private readonly ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> _baseServices;
        private IRequestExecutionQueue<RequestContext>? _queue;
        private readonly ImmutableArray<string> _supportedLanguages;
        private Task? _errorShutdownTask;

        public RoslynLanguageServerTarget(
            AbstractLspServiceProvider lspServiceProvider,
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            IAsynchronousOperationListenerProvider listenerProvider,
            IRoslynLspLogger logger,
            ImmutableArray<string> supportedLanguages,
            WellKnownLspServerKinds serverKind)
            : base(jsonRpc, capabilitiesProvider, logger, serverKind.ToConvertableString())
        {
            _lspServiceProvider = lspServiceProvider;
            _listener = listenerProvider.GetListener(FeatureAttribute.LanguageServer);

            // Create services that require base dependencies (jsonrpc) or are more complex to create to the set manually.
            _baseServices = GetBaseServices(jsonRpc, logger);
            _supportedLanguages = supportedLanguages;
        }

        public override IRequestExecutionQueue<RequestContext> GetRequestExecutionQueue()
        {
            if (_queue is null)
            {
                _queue = new RoslynRequestExecutionQueue(_supportedLanguages, _serverKind, GetLspServices(), _logger);
                _queue.RequestServerShutdown += RequestExecutionQueue_Errored;
            }

            return _queue;
        }

        protected override Task OnExitInternal()
        {
            ShutdownRequestQueue();
            return Task.CompletedTask;
        }

        public override ILspServices GetLspServices()
        {
            return _lspServiceProvider.CreateServices(_serverKind, _baseServices);
        }

        internal static ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> GetBaseServices(JsonRpc jsonRpc, IRoslynLspLogger logger)
        {
            var baseServices = ImmutableArray.Create(
                CreateLspServiceInstance<IClientLanguageServerManager>(new ClientLanguageServerManager(jsonRpc)),
                CreateLspServiceInstance(logger));

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

        public override InitializeParams? ClientSettings { get; }

        private void RequestExecutionQueue_Errored(object? sender, RequestShutdownEventArgs e)
        {
            // log message and shut down
            _logger?.TraceWarning($"Request queue is requesting shutdown due to error: {e.Message}");

            var message = new LogMessageParams()
            {
                MessageType = MessageType.Error,
                Message = e.Message
            };

            OnErroredStart();
            _errorShutdownTask = Task.Run(async () =>
            {
                _logger?.TraceInformation("Shutting down language server.");
                var lspServices = GetLspServices();
                var clientNotificationService = lspServices.GetRequiredService<IClientLanguageServerManager>();

                await clientNotificationService.SendNotificationAsync<LogMessageParams>(Methods.WindowLogMessageName, message, CancellationToken.None).ConfigureAwait(false);

                ShutdownImpl();
                await OnExitAsync();
            }).ContinueWith(OnErroredEndAsync);
        }

        protected override void ShutdownRequestQueue()
        {
            _queue.RequestServerShutdown -= RequestExecutionQueue_Errored;
            // if the queue requested shutdown via its event, it will have already shut itself down, but this
            // won't cause any problems calling it again
            _queue?.Shutdown();
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

            internal RoslynRequestExecutionQueue.TestAccessor GetQueueAccessor()
            {
                throw new NotImplementedException();
            }

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

    internal interface IRoslynLspLogger : ILspLogger, ILspService
    {
    }
}
