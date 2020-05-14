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

        public async Task<IClientConnection> ListenAsync(CancellationToken cancellationToken)
        {
            var pipeStream = await ListenCoreAsync(cancellationToken).ConfigureAwait(false);
            return new NamedPipeClientConnection(_compilerServerHost, _loggingIdentifier++.ToString(), pipeStream);
        }

        /// <summary>
        /// Creates a Task that waits for a client connection to occur and returns the connected 
        /// <see cref="NamedPipeServerStream"/> object.  Throws on any connection error.
        /// </summary>
        /// <param name="cancellationToken">Used to cancel the connection sequence.</param>
        private async Task<NamedPipeServerStream> ListenCoreAsync(CancellationToken cancellationToken)
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
}
