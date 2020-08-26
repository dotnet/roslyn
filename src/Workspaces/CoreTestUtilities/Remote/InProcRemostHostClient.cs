// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Nerdbank;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote.Testing
{
    internal sealed class InProcRemoteHostClient : RemoteHostClient, IRemoteHostServiceCallback
    {
        private readonly HostWorkspaceServices _services;
        private readonly InProcRemoteServices _inprocServices;
        private readonly RemoteEndPoint _endPoint;
        private readonly TraceSource _logger;

        public static async Task<RemoteHostClient> CreateAsync(HostWorkspaceServices services, RemoteHostTestData testData)
        {
            var inprocServices = new InProcRemoteServices(testData);

            var remoteHostStream = await inprocServices.RequestServiceAsync(WellKnownServiceHubService.RemoteHost).ConfigureAwait(false);

            var clientId = $"InProc ({Guid.NewGuid()})";
            var instance = new InProcRemoteHostClient(clientId, services, inprocServices, remoteHostStream);

            // make sure connection is done right
            var uiCultureLCIDE = 0;
            var cultureLCID = 0;

            await instance._endPoint.InvokeAsync(
                nameof(IRemoteHostService.InitializeGlobalState),
                new object?[] { uiCultureLCIDE, cultureLCID },
                CancellationToken.None).ConfigureAwait(false);

            instance.Started(Process.GetCurrentProcess().Id);

            // return instance
            return instance;
        }

        private InProcRemoteHostClient(
            string clientId,
            HostWorkspaceServices services,
            InProcRemoteServices inprocServices,
            Stream stream)
        {
            ClientId = clientId;
            _services = services;
            _logger = new TraceSource("Default");

            _inprocServices = inprocServices;

            _endPoint = new RemoteEndPoint(stream, _logger, incomingCallTarget: this);
            _endPoint.Disconnected += OnDisconnected;
            _endPoint.StartListening();
        }

        public static async Task<InProcRemoteHostClient> GetTestClientAsync(Workspace workspace)
        {
            var client = (InProcRemoteHostClient?)await TryGetClientAsync(workspace, CancellationToken.None).ConfigureAwait(false);
            Contract.ThrowIfNull(client);
            return client;
        }

        public RemoteWorkspace GetRemoteWorkspace()
            => TestData.WorkspaceManager.GetWorkspace();

        /// <summary>
        /// Remote API.
        /// </summary>
        public Task GetAssetsAsync(int scopeId, Checksum[] checksums, string pipeName, CancellationToken cancellationToken)
            => RemoteEndPoint.WriteDataToNamedPipeAsync(
                pipeName,
                (scopeId, checksums),
                (writer, data, cancellationToken) => RemoteHostAssetSerialization.WriteDataAsync(
                    writer, _services.GetRequiredService<ISolutionAssetStorageProvider>().AssetStorage, _services.GetRequiredService<ISerializerService>(), data.scopeId, data.checksums, cancellationToken),
                cancellationToken);

        /// <summary>
        /// Remote API.
        /// </summary>
        public Task<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken)
            => _services.GetRequiredService<IExperimentationService>().IsExperimentEnabled(experimentName) ? SpecializedTasks.True : SpecializedTasks.False;

        public RemoteHostTestData TestData => _inprocServices.TestData;

        public void RegisterService(RemoteServiceName serviceName, Func<Stream, IServiceProvider, ServiceBase> serviceCreator)
            => _inprocServices.RegisterService(serviceName, serviceCreator);

        public Task<Stream> RequestServiceAsync(RemoteServiceName serviceName)
            => _inprocServices.RequestServiceAsync(serviceName);

        public override string ClientId { get; }

        public override async Task<RemoteServiceConnection> CreateConnectionAsync(RemoteServiceName serviceName, object? callbackTarget, CancellationToken cancellationToken)
        {
            // get stream from service hub to communicate service specific information 
            // this is what consumer actually use to communicate information
            var serviceStream = await _inprocServices.RequestServiceAsync(serviceName).ConfigureAwait(false);

            return new JsonRpcConnection(_services, _logger, callbackTarget, serviceStream, poolReclamation: null);
        }

        public override void Dispose()
        {
            // we are asked to disconnect. unsubscribe and dispose to disconnect
            _endPoint.Disconnected -= OnDisconnected;
            _endPoint.Dispose();

            base.Dispose();
        }

        private void OnDisconnected(JsonRpcDisconnectedEventArgs e)
            => Dispose();

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

        private sealed class InProcRemoteServices
        {
            private readonly ServiceProvider _serviceProvider;
            private readonly Dictionary<RemoteServiceName, Func<Stream, IServiceProvider, ServiceBase>> _creatorMap;

            public InProcRemoteServices(RemoteHostTestData testData)
            {
                var remoteLogger = new TraceSource("inprocRemoteClient");

                _serviceProvider = new ServiceProvider(remoteLogger, testData);
                _creatorMap = new Dictionary<RemoteServiceName, Func<Stream, IServiceProvider, ServiceBase>>();

                RegisterService(WellKnownServiceHubService.RemoteHost, (s, p) => new RemoteHostService(s, p));
                RegisterService(WellKnownServiceHubService.CodeAnalysis, (s, p) => new CodeAnalysisService(s, p));
                RegisterService(WellKnownServiceHubService.RemoteSymbolSearchUpdateEngine, (s, p) => new RemoteSymbolSearchUpdateEngine(s, p));
                RegisterService(WellKnownServiceHubService.RemoteDesignerAttributeService, (s, p) => new RemoteDesignerAttributeService(s, p));
                RegisterService(WellKnownServiceHubService.RemoteProjectTelemetryService, (s, p) => new RemoteProjectTelemetryService(s, p));
                RegisterService(WellKnownServiceHubService.RemoteTodoCommentsService, (s, p) => new RemoteTodoCommentsService(s, p));
                RegisterService(WellKnownServiceHubService.LanguageServer, (s, p) => new LanguageServer(s, p));
            }

            public RemoteHostTestData TestData => _serviceProvider.TestData;

            public void RegisterService(RemoteServiceName name, Func<Stream, IServiceProvider, ServiceBase> serviceCreator)
                => _creatorMap.Add(name, serviceCreator);

            public Task<Stream> RequestServiceAsync(RemoteServiceName serviceName)
            {
                if (_creatorMap.TryGetValue(serviceName, out var creator))
                {
                    var tuple = FullDuplexStream.CreateStreams();
                    return Task.FromResult<Stream>(new WrappedStream(creator(tuple.Item1, _serviceProvider), tuple.Item2));
                }

                throw ExceptionUtilities.UnexpectedValue(serviceName);
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

#if NET5_0 // nullability annotations differ
                public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => _stream.BeginRead(buffer, offset, count, callback, state);
                public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => _stream.BeginWrite(buffer, offset, count, callback, state);
#else
                public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object? state) => _stream.BeginRead(buffer, offset, count, callback, state);
                public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object? state) => _stream.BeginWrite(buffer, offset, count, callback, state);
#endif
                public override int EndRead(IAsyncResult asyncResult) => _stream.EndRead(asyncResult);
                public override void EndWrite(IAsyncResult asyncResult) => _stream.EndWrite(asyncResult);

                public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => _stream.CopyToAsync(destination, bufferSize, cancellationToken);

                public override object InitializeLifetimeService()
                    => throw new NotSupportedException();

#if !NETCOREAPP
                public override ObjRef CreateObjRef(Type requestedType)
                    => throw new NotSupportedException();
#endif

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
