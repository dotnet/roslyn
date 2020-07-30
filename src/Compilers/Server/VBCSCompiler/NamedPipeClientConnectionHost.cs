// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using System.Security.AccessControl;

#nullable enable

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class NamedPipeClientConnectionHost : IClientConnectionHost
    {
        private int _clientCount;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task<IClientConnection>? _listenTask;

        internal string PipeName { get; }
        public bool IsListening { get; private set; }

        internal NamedPipeClientConnectionHost(string pipeName)
        {
            PipeName = pipeName;
        }

        public void BeginListening()
        {
            if (IsListening)
            {
                throw new InvalidOperationException();
            }

            IsListening = true;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void EndListening()
        {
            if (!IsListening)
            {
                throw new InvalidOperationException();
            }

            Debug.Assert(_cancellationTokenSource is object);
            try
            {
                _cancellationTokenSource.Cancel();

                if (_listenTask is object && !_listenTask.IsCompleted)
                {
                    try
                    {
                        _listenTask?.Wait();
                    }
                    catch
                    {
                        // It's expected the above Wait will cause exceptions to be thrown. 
                        // - When the named pipe server was actively listening the cancellation
                        //   will cause OperationCancelled to be thrown on the Wait call
                        // - When the named pipe server was in the middle of throwing an error
                        //   that will come through Wait.
                        // That is not meant to be handled here. The contract is we will ensure
                        // the last Task returned from 
                    }
                }
            }
            finally
            {
                IsListening = false;
                _cancellationTokenSource = null;
                _listenTask = null;
            }
        }

        public Task<IClientConnection> GetNextClientConnectionAsync()
        {
            if ((_listenTask is object && !_listenTask.IsCompleted) || !IsListening)
            {
                throw new InvalidOperationException();
            }

            Debug.Assert(_cancellationTokenSource is object);

            var clientLoggingIdentifier = $"Client{_clientCount++}";
            _listenTask = Task.Run(() => ListenCoreAsync(PipeName, clientLoggingIdentifier, _cancellationTokenSource.Token));
            return _listenTask;
        }

        /// <summary>
        /// Creates a Task that waits for a client connection to occur and returns the connected 
        /// <see cref="NamedPipeServerStream"/> object.  Throws on any connection error.
        /// </summary>
        /// <param name="cancellationToken">Used to cancel the connection sequence.</param>
        private static async Task<IClientConnection> ListenCoreAsync(string pipeName, string clientLoggingIdentifier, CancellationToken cancellationToken)
        {
            // Create the pipe and begin waiting for a connection. This 
            // doesn't block, but could fail in certain circumstances, such
            // as Windows refusing to create the pipe for some reason 
            // (out of handles?), or the pipe was disconnected before we 
            // starting listening.
            CompilerServerLogger.Log("Constructing pipe '{0}'.", pipeName);
            var pipeStream = NamedPipeUtil.CreateServer(pipeName);
            CompilerServerLogger.Log("Successfully constructed pipe '{0}'.", pipeName);

            CompilerServerLogger.Log("Waiting for new connection");
            await pipeStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
            CompilerServerLogger.Log("Pipe connection detected.");

            return new NamedPipeClientConnection(pipeStream, clientLoggingIdentifier);
        }
    }
}
