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
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class RoslynLanguageServerTarget : LanguageServerTarget<RequestContext>, IClientCapabilitiesProvider
    {
        private readonly ICapabilitiesProvider _capabilitiesProvider;

        private readonly AbstractLspServiceProvider _lspServiceProvider;
        private readonly IAsynchronousOperationListener _listener;
        private IAsyncToken? _erroredToken;
        private readonly ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> _baseServices;
        private readonly ImmutableArray<string> _supportedLanguages;
        private Task? _errorShutdownTask;
        private IRequestDispatcher<RequestContext>? _requestDispatcher;
        private LspServices? _lspServices;

        public RoslynLanguageServerTarget(
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
            _baseServices = GetBaseServices(jsonRpc, this, logger, capabilitiesProvider);
            _supportedLanguages = supportedLanguages;

            Initialize();
        }

        public override ILspServices GetLspServices()
        {
            if (_lspServices is null)
            {
                _lspServices = _lspServiceProvider.CreateServices(_serverKind, _baseServices);
            }
            return _lspServices;
        }

        internal static ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> GetBaseServices(
            JsonRpc jsonRpc,
            IClientCapabilitiesProvider clientCapabilitiesProvider,
            IRoslynLspLogger logger,
            ICapabilitiesProvider capabilitiesProvider)
        {
            var baseServices = ImmutableArray.Create(
                CreateLspServiceInstance<IClientLanguageServerManager>(new ClientLanguageServerManager(jsonRpc)),
                CreateLspServiceInstance(logger),
                CreateLspServiceInstance<IClientCapabilitiesProvider>(clientCapabilitiesProvider),
                CreateLspServiceInstance<ICapabilitiesProvider>(capabilitiesProvider));

            return baseServices;

            static Lazy<ILspService, LspServiceMetadataView> CreateLspServiceInstance<T>(T lspServiceInstance) where T : ILspService
            {
                return new Lazy<ILspService, LspServiceMetadataView>(
                    () => lspServiceInstance, new LspServiceMetadataView(typeof(T)));
            }
        }

        public override RequestExecutionQueue<RequestContext> GetRequestExecutionQueue()
        {
            var lspServices = GetLspServices();
            var requestExecutionQueue = new RoslynRequestExecutionQueue(_serverKind, lspServices, _logger);
            requestExecutionQueue.SetSupportedLanguages(_supportedLanguages);

            return requestExecutionQueue;
        }

        public override IRequestDispatcher<RequestContext> GetRequestDispatcher()
        {
            if (_requestDispatcher is null)
            {
                var lspServices = GetLspServices();
                _requestDispatcher = lspServices.GetRequiredService<RoslynRequestDispatcher>();

                SetupRequestDispatcher(_requestDispatcher);
            }

            return _requestDispatcher;
        }

        //protected override Task OnExitInternal()
        //{
        //    ShutdownRequestQueue();
        //    return Task.CompletedTask;
        //}

        //public override void OnErroredStart()
        //{
        //    _erroredToken = _listener.BeginAsyncOperation("RequestExecutionQueue_Errored");
        //    // log message and shut down
        //    _logger?.TraceWarning($"Request queue is requesting shutdown due to error: {e.Message}");

        //    var message = new LogMessageParams()
        //    {
        //        MessageType = MessageType.Error,
        //        Message = e.Message
        //    };

        //    OnErroredStart();
        //    _errorShutdownTask = Task.Run(async () =>
        //    {
        //        _logger?.TraceInformation("Shutting down language server.");
        //        var lspServices = GetLspServices();
        //        var clientNotificationService = lspServices.GetRequiredService<IClientLanguageServerManager>();

        //        await clientNotificationService.SendNotificationAsync<LogMessageParams>(Methods.WindowLogMessageName, message, CancellationToken.None).ConfigureAwait(false);

        //        ShutdownImpl();
        //        await OnExitAsync();
        //    }).ContinueWith(OnErroredEndAsync);
        //}

        public ClientCapabilities GetClientCapabilities()
        {
            var lspServices = GetLspServices();
            var clientCapabilitiesManager = lspServices.GetRequiredService<IClientCapabilitiesManager>();
            var clientCapabilities = clientCapabilitiesManager.GetClientCapabilities();

            return clientCapabilities;
        }

        public override Task OnErroredEndAsync(object obj)
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

    internal interface IRoslynLspLogger : ILspLogger, ILspService
    {
    }
}
