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
        private readonly string _pipeName;
        private int _clientLoggingIdentifier;

        internal NamedPipeClientConnectionHost(string pipeName)
        {
            _pipeName = pipeName;
        }

        /// <summary>
        /// Creates a Task that waits for a client connection to occur and returns the connected 
        /// <see cref="NamedPipeServerStream"/> object.  Throws on any connection error.
        /// </summary>
        /// <param name="cancellationToken">Used to cancel the connection sequence.</param>
        public async Task<IClientConnection> ListenAsync(CancellationToken cancellationToken)
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
                var clientLoggingIdentifier = $"Client{_clientLoggingIdentifier++}";
                return new NamedPipeClientConnection(pipeStream, clientLoggingIdentifier);
            }

            pipeStream.Close();
            throw new Exception("Insufficient resources to process new connection.");
        }
    }
}
