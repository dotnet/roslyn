// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Nerdbank;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Roslyn.Test.Utilities.Remote
{
    internal class InProcRemoteHostClient : RemoteHostClient
    {
        private readonly InProcRemoteServices _inprocServices;
        private readonly Stream _stream;
        private readonly JsonRpc _rpc;

        public static async Task<RemoteHostClient> CreateAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            var inprocServices = new InProcRemoteServices();

            var remoteHostStream = await inprocServices.RequestServiceAsync(WellKnownRemoteHostServices.RemoteHostService, cancellationToken).ConfigureAwait(false);

            var instance = new InProcRemoteHostClient(workspace, inprocServices, remoteHostStream);

            // make sure connection is done right
            var current = $"VS ({Process.GetCurrentProcess().Id})";
            var host = await instance._rpc.InvokeAsync<string>(WellKnownRemoteHostServices.RemoteHostService_Connect, current).ConfigureAwait(false);

            // TODO: change this to non fatal watson and make VS to use inproc implementation
            Contract.ThrowIfFalse(host == current.ToString());

            instance.Connected();

            // return instance
            return instance;
        }

        private InProcRemoteHostClient(Workspace workspace, InProcRemoteServices inprocServices, Stream stream) :
            base(workspace)
        {
            _inprocServices = inprocServices;

            _stream = stream;
            _rpc = JsonRpc.Attach(stream, target: this);

            // handle disconnected situation
            _rpc.Disconnected += OnRpcDisconnected;
        }

        protected override async Task<Session> CreateServiceSessionAsync(string serviceName, ChecksumScope snapshot, object callbackTarget, CancellationToken cancellationToken)
        {
            // get stream from service hub to communicate snapshot/asset related information
            // this is the back channel the system uses to move data between VS and remote host
            var snapshotStream = await _inprocServices.RequestServiceAsync(WellKnownServiceHubServices.SnapshotService, cancellationToken).ConfigureAwait(false);

            // get stream from service hub to communicate service specific information
            // this is what consumer actually use to communicate information
            var serviceStream = await _inprocServices.RequestServiceAsync(serviceName, cancellationToken).ConfigureAwait(false);

            return new JsonRpcSession(snapshot, snapshotStream, callbackTarget, serviceStream, cancellationToken);
        }

        protected override void OnConnected()
        {
        }

        protected override void OnDisconnected()
        {
            _rpc.Dispose();
            _stream.Dispose();
        }

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            Disconnected();
        }

        private class InProcRemoteServices
        {
            private static readonly IServiceProvider s_serviceProvider = new ServiceProvider();

            public InProcRemoteServices()
            {
            }

            public Task<Stream> RequestServiceAsync(string serviceName, CancellationToken cancellationToken)
            {
                switch (serviceName)
                {
                    case WellKnownRemoteHostServices.RemoteHostService:
                        {
                            var tuple = FullDuplexStream.CreateStreams();
                            return Task.FromResult<Stream>(new WrappedStream(new RemoteHostService(tuple.Item1, s_serviceProvider), tuple.Item2));
                        }
                    case WellKnownServiceHubServices.CodeAnalysisService:
                        {
                            var tuple = FullDuplexStream.CreateStreams();
                            return Task.FromResult<Stream>(new WrappedStream(new CodeAnalysisService(tuple.Item1, s_serviceProvider), tuple.Item2));
                        }
                    case WellKnownServiceHubServices.SnapshotService:
                        {
                            var tuple = FullDuplexStream.CreateStreams();
                            return Task.FromResult<Stream>(new WrappedStream(new SnapshotService(tuple.Item1, s_serviceProvider), tuple.Item2));
                        }
                    case WellKnownServiceHubServices.RemoteSymbolSearchUpdateEngine:
                        {
                            var tuple = FullDuplexStream.CreateStreams();
                            return Task.FromResult<Stream>(new WrappedStream(new RemoteSymbolSearchUpdateEngine(tuple.Item1, s_serviceProvider), tuple.Item2));
                        }
                }

                throw ExceptionUtilities.UnexpectedValue(serviceName);
            }

            private class ServiceProvider : IServiceProvider
            {
                private static readonly TraceSource s_traceSource = new TraceSource("inprocRemoteClient");

                public object GetService(Type serviceType)
                {
                    if (typeof(TraceSource) == serviceType)
                    {
                        return s_traceSource;
                    }

                    throw ExceptionUtilities.UnexpectedValue(serviceType);
                }
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