﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class DirectStream
    {
        public static async Task<Stream> GetAsync(string streamName, CancellationToken cancellationToken)
        {
            var stream = new ClientDirectStream(streamName);

            try
            {
                // try to connect direct stream
                await stream.ConnectAsync(cancellationToken).ConfigureAwait(false);
                return stream;
            }
            catch
            {
                // make sure we dispose stream in case ConnectAsync failed
                stream.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Direct stream between server and client to pass around big chunk of data
        /// </summary>
        private class ClientDirectStream : Stream
        {
            // 4KB buffer size
            private const int BUFFERSIZE = 4 * 1024;

            private readonly string _name;
            private readonly NamedPipeClientStream _pipe;
            private readonly Stream _stream;

            public ClientDirectStream(string name)
            {
                // this type exists so that consumer doesn't need to care about all these arguments/flags to get good performance
                _name = name;
                _pipe = new NamedPipeClientStream(serverName: ".", pipeName: name, direction: PipeDirection.Out);
                _stream = new BufferedStream(_pipe, BUFFERSIZE);
            }

            public string Name => _name;

            public Task ConnectAsync(CancellationToken cancellationToken)
            {
                return _pipe.ConnectAsync(cancellationToken);
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

            public override void Close()
            {
                if (_pipe.IsConnected)
                {
                    // calling close on disconnected pipe will
                    // throw an exception
                    _stream.Close();
                }
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

            protected override void Dispose(bool disposing) => _stream.Dispose();

            public override object InitializeLifetimeService()
            {
                throw new NotSupportedException();
            }

            public override ObjRef CreateObjRef(Type requestedType)
            {
                throw new NotSupportedException();
            }
        }
    }
}
