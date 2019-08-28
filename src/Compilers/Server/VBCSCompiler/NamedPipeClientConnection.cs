// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class NamedPipeClientConnectionHost : IClientConnectionHost
    {
        private readonly ICompilerServerHost _compilerServerHost;
        private readonly string _pipeName;
        private int _loggingIdentifier;

        internal NamedPipeClientConnectionHost(ICompilerServerHost compilerServerHost, string pipeName)
        {
            _compilerServerHost = compilerServerHost;
            _pipeName = pipeName;
        }

        public async Task<IClientConnection> CreateListenTask(CancellationToken cancellationToken)
        {
            var pipeStream = await CreateListenTaskCore(cancellationToken).ConfigureAwait(false);
            return new NamedPipeClientConnection(_compilerServerHost, _loggingIdentifier++.ToString(), pipeStream);
        }

        /// <summary>
        /// Creates a Task that waits for a client connection to occur and returns the connected 
        /// <see cref="NamedPipeServerStream"/> object.  Throws on any connection error.
        /// </summary>
        /// <param name="cancellationToken">Used to cancel the connection sequence.</param>
        private async Task<NamedPipeServerStream> CreateListenTaskCore(CancellationToken cancellationToken)
        {
            // Create the pipe and begin waiting for a connection. This 
            // doesn't block, but could fail in certain circumstances, such
            // as Windows refusing to create the pipe for some reason 
            // (out of handles?), or the pipe was disconnected before we 
            // starting listening.
            CompilerServerLogger.Log("Constructing pipe '{0}'.", _pipeName);
            var pipeStream = NamedPipeUtil.CreateServer(_pipeName);
            CompilerServerLogger.Log("Successfully constructed pipe '{0}'.", _pipeName);

            CompilerServerLogger.Log("Waiting for new connection");
            await pipeStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
            CompilerServerLogger.Log("Pipe connection detected.");

            if (Environment.Is64BitProcess || MemoryHelper.IsMemoryAvailable())
            {
                CompilerServerLogger.Log("Memory available - accepting connection");
                return pipeStream;
            }

            pipeStream.Close();
            throw new Exception("Insufficient resources to process new connection.");
        }
    }

    internal sealed class NamedPipeClientConnection : ClientConnection
    {
        private readonly NamedPipeServerStream _pipeStream;

        internal NamedPipeClientConnection(ICompilerServerHost compilerServerHost, string loggingIdentifier, NamedPipeServerStream pipeStream)
            : base(compilerServerHost, loggingIdentifier, pipeStream)
        {
            _pipeStream = pipeStream;
        }

        /// <summary>
        /// The IsConnected property on named pipes does not detect when the client has disconnected
        /// if we don't attempt any new I/O after the client disconnects. We start an async I/O here
        /// which serves to check the pipe for disconnection. 
        ///
        /// This will return true if the pipe was disconnected.
        /// </summary>
        protected override Task CreateMonitorDisconnectTask(CancellationToken cancellationToken)
        {
            return BuildServerConnection.CreateMonitorDisconnectTask(_pipeStream, LoggingIdentifier, cancellationToken);
        }

        protected override void ValidateBuildRequest(BuildRequest request)
        {
            // Now that we've read data from the stream we can validate the identity.
            if (!NamedPipeUtil.CheckClientElevationMatches(_pipeStream))
            {
                throw new Exception("Client identity does not match server identity.");
            }
        }

        public override void Close()
        {
            CompilerServerLogger.Log($"Pipe {LoggingIdentifier}: Closing.");
            try
            {
                _pipeStream.Close();
            }
            catch (Exception e)
            {
                // The client connection failing to close isn't fatal to the server process.  It is simply a client
                // for which we can no longer communicate and that's okay because the Close method indicates we are
                // done with the client already.
                var msg = string.Format($"Pipe {LoggingIdentifier}: Error closing pipe.");
                CompilerServerLogger.LogException(e, msg);
            }
        }
    }
}
