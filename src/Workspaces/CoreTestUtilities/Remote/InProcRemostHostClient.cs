// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.ServiceHub.Framework;
using Nerdbank.Streams;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Testing
{
    internal sealed partial class InProcRemoteHostClient : RemoteHostClient
    {
        private readonly SolutionServices _workspaceServices;
        private readonly InProcRemoteServices _inprocServices;
        private readonly RemoteServiceCallbackDispatcherRegistry _callbackDispatchers;

        public static RemoteHostClient Create(SolutionServices services, RemoteServiceCallbackDispatcherRegistry callbackDispatchers, TraceListener? traceListener, RemoteHostTestData testData)
        {
            var inprocServices = new InProcRemoteServices(services, traceListener, testData);
            var instance = new InProcRemoteHostClient(services, inprocServices, callbackDispatchers);

            // return instance
            return instance;
        }

        private InProcRemoteHostClient(
            SolutionServices services,
            InProcRemoteServices inprocServices,
            RemoteServiceCallbackDispatcherRegistry callbackDispatchers)
        {
            _workspaceServices = services;
            _callbackDispatchers = callbackDispatchers;
            _inprocServices = inprocServices;
        }

        public static async Task<InProcRemoteHostClient> GetTestClientAsync(Workspace workspace)
        {
            var client = (InProcRemoteHostClient?)await TryGetClientAsync(workspace, CancellationToken.None).ConfigureAwait(false);
            Contract.ThrowIfNull(client);
            return client;
        }

        public RemoteWorkspace GetRemoteWorkspace()
            => TestData.WorkspaceManager.GetWorkspace();

        public RemoteHostTestData TestData => _inprocServices.TestData;

        public override RemoteServiceConnection<T> CreateConnection<T>(object? callbackTarget) where T : class
        {
            var descriptor = ServiceDescriptors.Instance.GetServiceDescriptor(typeof(T), RemoteProcessConfiguration.ServerGC);
            var callbackDispatcher = (descriptor.ClientInterface != null) ? _callbackDispatchers.GetDispatcher(typeof(T)) : null;

            return new BrokeredServiceConnection<T>(
                descriptor,
                callbackTarget,
                callbackDispatcher,
                _inprocServices.ServiceBrokerClient,
                _workspaceServices.GetRequiredService<ISolutionAssetStorageProvider>().AssetStorage,
                _workspaceServices.GetRequiredService<IErrorReportingService>(),
                shutdownCancellationService: null,
                remoteProcess: null);
        }

        public override void Dispose()
        {
            _inprocServices.Dispose();
        }

        public sealed class ServiceProvider : IServiceProvider
        {
            public readonly TraceSource TraceSource;
            public readonly RemoteHostTestData TestData;

            public ServiceProvider(TraceSource traceSource, RemoteHostTestData testData)
            {
                TraceSource = traceSource;
                TestData = testData;
            }

            public object GetService(Type serviceType)
            {
                if (typeof(TraceSource) == serviceType)
                {
                    return TraceSource;
                }

                if (typeof(RemoteHostTestData) == serviceType)
                {
                    return TestData;
                }

                throw ExceptionUtilities.UnexpectedValue(serviceType);
            }
        }

        private sealed class InProcServiceBroker : IServiceBroker
        {
            private readonly InProcRemoteServices _services;

            public InProcServiceBroker(InProcRemoteServices services)
            {
                _services = services;
            }

            public event EventHandler<BrokeredServicesChangedEventArgs>? AvailabilityChanged { add { } remove { } }

            // This method is currently not needed for our IServiceBroker usage patterns.
            public ValueTask<IDuplexPipe?> GetPipeAsync(ServiceMoniker serviceMoniker, ServiceActivationOptions options, CancellationToken cancellationToken)
                => throw ExceptionUtilities.Unreachable();

            public ValueTask<T?> GetProxyAsync<T>(ServiceRpcDescriptor descriptor, ServiceActivationOptions options, CancellationToken cancellationToken) where T : class
            {
                var pipePair = FullDuplexStream.CreatePipePair();

                var clientConnection = descriptor
                    .WithTraceSource(_services.ServiceProvider.TraceSource)
                    .ConstructRpcConnection(pipePair.Item2);

                Contract.ThrowIfFalse(options.ClientRpcTarget is null == descriptor.ClientInterface is null);

                if (descriptor.ClientInterface != null)
                {
                    Contract.ThrowIfNull(options.ClientRpcTarget);
                    clientConnection.AddLocalRpcTarget(options.ClientRpcTarget);
                }

                // Clear RPC target so that the server connection is forced to create a new proxy for the callback
                // instead of just invoking the callback object directly (this emulates the product that does
                // not serialize the callback object over).
                options.ClientRpcTarget = null;

                // Creates service instance and connects it to the pipe. 
                // We don't need to store the instance anywhere.
                _ = _services.CreateBrokeredService(descriptor, pipePair.Item1, options);

                clientConnection.StartListening();

                return ValueTaskFactory.FromResult((T?)clientConnection.ConstructRpcClient<T>());
            }
        }

        private sealed class InProcRemoteServices : IDisposable
        {
            public readonly ServiceProvider ServiceProvider;
            private readonly Dictionary<ServiceMoniker, Func<object>> _inProcBrokeredServicesMap = [];
            private readonly Dictionary<ServiceMoniker, BrokeredServiceBase.IFactory> _remoteBrokeredServicesMap = [];

            public readonly IServiceBroker ServiceBroker;
            public readonly ServiceBrokerClient ServiceBrokerClient;

            public InProcRemoteServices(SolutionServices workspaceServices, TraceListener? traceListener, RemoteHostTestData testData)
            {
                var remoteLogger = new TraceSource("InProcRemoteClient")
                {
                    Switch = { Level = SourceLevels.Warning },
                };

                if (traceListener != null)
                {
                    remoteLogger.Listeners.Add(traceListener);
                }

                ServiceProvider = new ServiceProvider(remoteLogger, testData);

                ServiceBroker = new InProcServiceBroker(this);
#pragma warning disable VSTHRD012 // Provide JoinableTaskFactory where allowed
                ServiceBrokerClient = new ServiceBrokerClient(ServiceBroker);
#pragma warning restore

                RegisterInProcBrokeredService(SolutionAssetProvider.ServiceDescriptor, () => new SolutionAssetProvider(workspaceServices));
                RegisterRemoteBrokeredService(new RemoteAssetSynchronizationService.Factory());
                RegisterRemoteBrokeredService(new RemoteAsynchronousOperationListenerService.Factory());
                RegisterRemoteBrokeredService(new RemoteCodeLensReferencesService.Factory());
                RegisterRemoteBrokeredService(new RemoteConvertTupleToStructCodeRefactoringService.Factory());
                RegisterRemoteBrokeredService(new RemoteDependentTypeFinderService.Factory());
                RegisterRemoteBrokeredService(new RemoteDesignerAttributeDiscoveryService.Factory());
                RegisterRemoteBrokeredService(new RemoteDiagnosticAnalyzerService.Factory());
                RegisterRemoteBrokeredService(new RemoteDocumentHighlightsService.Factory());
                RegisterRemoteBrokeredService(new RemoteEditAndContinueService.Factory());
                RegisterRemoteBrokeredService(new RemoteEncapsulateFieldService.Factory());
                RegisterRemoteBrokeredService(new RemoteExtensionMethodImportCompletionService.Factory());
                RegisterRemoteBrokeredService(new RemoteFindUsagesService.Factory());
                RegisterRemoteBrokeredService(new RemoteFullyQualifyService.Factory());
                RegisterRemoteBrokeredService(new RemoteGlobalNotificationDeliveryService.Factory());
                RegisterRemoteBrokeredService(new RemoteInheritanceMarginService.Factory());
                RegisterRemoteBrokeredService(new RemoteKeepAliveService.Factory());
                RegisterRemoteBrokeredService(new RemoteLegacySolutionEventsAggregationService.Factory());
                RegisterRemoteBrokeredService(new RemoteMissingImportDiscoveryService.Factory());
                RegisterRemoteBrokeredService(new RemoteNavigateToSearchService.Factory());
                RegisterRemoteBrokeredService(new RemoteNavigationBarItemService.Factory());
                RegisterRemoteBrokeredService(new RemoteProcessTelemetryService.Factory());
                RegisterRemoteBrokeredService(new RemoteRenamerService.Factory());
                RegisterRemoteBrokeredService(new RemoteSemanticClassificationService.Factory());
                RegisterRemoteBrokeredService(new RemoteSemanticSearchService.Factory());
                RegisterRemoteBrokeredService(new RemoteSourceGenerationService.Factory());
                RegisterRemoteBrokeredService(new RemoteStackTraceExplorerService.Factory());
                RegisterRemoteBrokeredService(new RemoteSymbolFinderService.Factory());
                RegisterRemoteBrokeredService(new RemoteSymbolSearchUpdateService.Factory());
                RegisterRemoteBrokeredService(new RemoteTaskListService.Factory());
                RegisterRemoteBrokeredService(new RemoteUnitTestingSearchService.Factory());
                RegisterRemoteBrokeredService(new RemoteUnusedReferenceAnalysisService.Factory());
                RegisterRemoteBrokeredService(new RemoteValueTrackingService.Factory());
            }

            public void Dispose()
                => ServiceBrokerClient.Dispose();

            public RemoteHostTestData TestData => ServiceProvider.TestData;

            public void RegisterInProcBrokeredService(ServiceDescriptor serviceDescriptor, Func<object> serviceFactory)
            {
                _inProcBrokeredServicesMap.Add(serviceDescriptor.Moniker, serviceFactory);
            }

            public void RegisterRemoteBrokeredService(BrokeredServiceBase.IFactory serviceFactory)
            {
                var moniker = ServiceDescriptors.Instance.GetServiceDescriptorForServiceFactory(serviceFactory.ServiceType).Moniker;
                _remoteBrokeredServicesMap.Add(moniker, serviceFactory);
            }

            public object CreateBrokeredService(ServiceRpcDescriptor descriptor, IDuplexPipe pipe, ServiceActivationOptions options)
            {
                if (_inProcBrokeredServicesMap.TryGetValue(descriptor.Moniker, out var inProcFactory))
                {
                    // This code is similar to service creation implemented in BrokeredServiceBase.FactoryBase.
                    // Currently don't support callback creation as we don't have in-proc service with callbacks yet.
                    Contract.ThrowIfFalse(descriptor.ClientInterface == null);

                    var serviceConnection = descriptor.WithTraceSource(ServiceProvider.TraceSource).ConstructRpcConnection(pipe);
                    var service = inProcFactory();

                    serviceConnection.AddLocalRpcTarget(service);
                    serviceConnection.StartListening();

                    return service;
                }

                if (_remoteBrokeredServicesMap.TryGetValue(descriptor.Moniker, out var remoteFactory))
                {
                    return remoteFactory.Create(pipe, ServiceProvider, options, ServiceBroker);
                }

                throw ExceptionUtilities.UnexpectedValue(descriptor.Moniker);
            }

            private sealed class WrappedStream : Stream
            {
                private readonly IDisposable _service;
                private readonly Stream _stream;

                public WrappedStream(IDisposable service, Stream stream)
                {
                    // tie service's lifetime with that of stream
                    _service = service;
                    _stream = stream;
                }

                public override long Position
                {
                    get { return _stream.Position; }
                    set { _stream.Position = value; }
                }

                public override int ReadTimeout
                {
                    get { return _stream.ReadTimeout; }
                    set { _stream.ReadTimeout = value; }
                }

                public override int WriteTimeout
                {
                    get { return _stream.WriteTimeout; }
                    set { _stream.WriteTimeout = value; }
                }

                public override bool CanRead => _stream.CanRead;
                public override bool CanSeek => _stream.CanSeek;
                public override bool CanWrite => _stream.CanWrite;
                public override long Length => _stream.Length;
                public override bool CanTimeout => _stream.CanTimeout;

                public override void Flush() => _stream.Flush();
                public override Task FlushAsync(CancellationToken cancellationToken) => _stream.FlushAsync(cancellationToken);

                public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
                public override void SetLength(long value) => _stream.SetLength(value);

                public override int ReadByte() => _stream.ReadByte();
                public override void WriteByte(byte value) => _stream.WriteByte(value);

                public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);
                public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);

                public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _stream.ReadAsync(buffer, offset, count, cancellationToken);
                public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _stream.WriteAsync(buffer, offset, count, cancellationToken);

#if NETCOREAPP // nullability annotations differ
                public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => _stream.BeginRead(buffer, offset, count, callback, state);
                public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => _stream.BeginWrite(buffer, offset, count, callback, state);
#else
                public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object? state) => _stream.BeginRead(buffer, offset, count, callback, state);
                public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object? state) => _stream.BeginWrite(buffer, offset, count, callback, state);
#endif
                public override int EndRead(IAsyncResult asyncResult) => _stream.EndRead(asyncResult);
                public override void EndWrite(IAsyncResult asyncResult) => _stream.EndWrite(asyncResult);

                public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => _stream.CopyToAsync(destination, bufferSize, cancellationToken);

                public override void Close()
                {
                    _service.Dispose();
                    _stream.Close();
                }

                protected override void Dispose(bool disposing)
                {
                    base.Dispose(disposing);

                    _service.Dispose();
                    _stream.Dispose();
                }
            }
        }
    }
}
