// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Nerdbank;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Roslyn.Test.Utilities.Remote
{
    internal sealed class InProcRemoteHostClient : RemoteHostClient
    {
        private readonly InProcRemoteServices _inprocServices;
        private readonly ReferenceCountedDisposable<RemotableDataJsonRpc> _remotableDataRpc;
        private readonly JsonRpc _rpc;

        public static async Task<RemoteHostClient> CreateAsync(Workspace workspace, bool runCacheCleanup)
        {
            var inprocServices = new InProcRemoteServices(runCacheCleanup);

            // Create the RemotableDataJsonRpc before we create the remote host: this call implicitly sets up the remote IExperimentationService so that will be available for later calls
            var remotableDataRpc = new RemotableDataJsonRpc(workspace, inprocServices.Logger, await inprocServices.RequestServiceAsync(WellKnownServiceHubServices.SnapshotService).ConfigureAwait(false));
            var remoteHostStream = await inprocServices.RequestServiceAsync(WellKnownRemoteHostServices.RemoteHostService).ConfigureAwait(false);

            var current = CreateClientId(Process.GetCurrentProcess().Id.ToString());
            var instance = new InProcRemoteHostClient(current, workspace, inprocServices, new ReferenceCountedDisposable<RemotableDataJsonRpc>(remotableDataRpc), remoteHostStream);

            // make sure connection is done right
            var telemetrySession = default(string);
            var uiCultureLCIDE = 0;
            var cultureLCID = 0;

            var host = await instance._rpc.InvokeAsync<string>(nameof(IRemoteHostService.Connect), current, uiCultureLCIDE, cultureLCID, telemetrySession).ConfigureAwait(false);

            // TODO: change this to non fatal watson and make VS to use inproc implementation
            Contract.ThrowIfFalse(host == current.ToString());

            instance.Started();

            // return instance
            return instance;
        }

        private InProcRemoteHostClient(
            string clientId,
            Workspace workspace,
            InProcRemoteServices inprocServices,
            ReferenceCountedDisposable<RemotableDataJsonRpc> remotableDataRpc,
            Stream stream)
            : base(workspace)
        {
            Contract.ThrowIfNull(remotableDataRpc);

            ClientId = clientId;

            _inprocServices = inprocServices;
            _remotableDataRpc = remotableDataRpc;

            _rpc = stream.CreateStreamJsonRpc(target: this, inprocServices.Logger);

            // handle disconnected situation
            _rpc.Disconnected += OnRpcDisconnected;

            _rpc.StartListening();
        }

        public AssetStorage AssetStorage => _inprocServices.AssetStorage;

        public void RegisterService(string name, Func<Stream, IServiceProvider, ServiceHubServiceBase> serviceCreator)
        {
            _inprocServices.RegisterService(name, serviceCreator);
        }

        public override string ClientId { get; }

        public override async Task<Connection> TryCreateConnectionAsync(
            string serviceName, object callbackTarget, CancellationToken cancellationToken)
        {
            // get stream from service hub to communicate service specific information 
            // this is what consumer actually use to communicate information
            var serviceStream = await _inprocServices.RequestServiceAsync(serviceName).ConfigureAwait(false);

            return new JsonRpcConnection(_inprocServices.Logger, callbackTarget, serviceStream, _remotableDataRpc.TryAddReference());
        }

        protected override void OnStarted()
        {
        }

        protected override void OnStopped()
        {
            // we are asked to disconnect. unsubscribe and dispose to disconnect
            _rpc.Disconnected -= OnRpcDisconnected;
            _rpc.Dispose();
            _remotableDataRpc.Dispose();
        }

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            Stopped();
        }

        public class ServiceProvider : IServiceProvider
        {
            private static readonly TraceSource s_traceSource = new TraceSource("inprocRemoteClient");

            private readonly AssetStorage _storage;

            public ServiceProvider(bool runCacheCleanup)
            {
                _storage = runCacheCleanup ?
                    new AssetStorage(cleanupInterval: TimeSpan.FromSeconds(30), purgeAfter: TimeSpan.FromMinutes(1), gcAfter: TimeSpan.FromMinutes(5)) :
                    new AssetStorage();
            }

            public AssetStorage AssetStorage => _storage;

            public object GetService(Type serviceType)
            {
                if (typeof(TraceSource) == serviceType)
                {
                    return s_traceSource;
                }

                if (typeof(AssetStorage) == serviceType)
                {
                    return _storage;
                }

                throw ExceptionUtilities.UnexpectedValue(serviceType);
            }
        }

        private class InProcRemoteServices
        {
            private readonly ServiceProvider _serviceProvider;
            private readonly Dictionary<string, Func<Stream, IServiceProvider, ServiceHubServiceBase>> _creatorMap;

            public InProcRemoteServices(bool runCacheCleanup)
            {
                _serviceProvider = new ServiceProvider(runCacheCleanup);
                _creatorMap = new Dictionary<string, Func<Stream, IServiceProvider, ServiceHubServiceBase>>();

                RegisterService(WellKnownRemoteHostServices.RemoteHostService, (s, p) => new RemoteHostService(s, p));
                RegisterService(WellKnownServiceHubServices.CodeAnalysisService, (s, p) => new CodeAnalysisService(s, p));
                RegisterService(WellKnownServiceHubServices.SnapshotService, (s, p) => new SnapshotService(s, p));
                RegisterService(WellKnownServiceHubServices.RemoteSymbolSearchUpdateEngine, (s, p) => new RemoteSymbolSearchUpdateEngine(s, p));
            }

            public AssetStorage AssetStorage => _serviceProvider.AssetStorage;
            public TraceSource Logger { get; } = new TraceSource("Default");

            public void RegisterService(string name, Func<Stream, IServiceProvider, ServiceHubServiceBase> serviceCreator)
            {
                _creatorMap.Add(name, serviceCreator);
            }

            public Task<Stream> RequestServiceAsync(string serviceName)
            {
                if (_creatorMap.TryGetValue(serviceName, out var creator))
                {
                    var tuple = FullDuplexStream.CreateStreams();
                    return Task.FromResult<Stream>(new WrappedStream(creator(tuple.Item1, _serviceProvider), tuple.Item2));
                }

                throw ExceptionUtilities.UnexpectedValue(serviceName);
            }

            private class WrappedStream : Stream
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

                public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => _stream.BeginRead(buffer, offset, count, callback, state);
                public override int EndRead(IAsyncResult asyncResult) => _stream.EndRead(asyncResult);

                public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => _stream.BeginWrite(buffer, offset, count, callback, state);
                public override void EndWrite(IAsyncResult asyncResult) => _stream.EndWrite(asyncResult);

                public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => _stream.CopyToAsync(destination, bufferSize, cancellationToken);

                public override object InitializeLifetimeService()
                {
                    throw new NotSupportedException();
                }

                public override ObjRef CreateObjRef(Type requestedType)
                {
                    throw new NotSupportedException();
                }

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
